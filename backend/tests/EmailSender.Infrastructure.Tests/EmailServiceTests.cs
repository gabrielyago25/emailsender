using EmailSender.Core.Models;
using EmailSender.Core.Services;
using EmailSender.Infrastructure.Configuration;
using EmailSender.Infrastructure.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;

namespace EmailSender.Infrastructure.Tests;

public class EmailServiceTests
{
    private readonly Mock<ISmtpClient> _client = new(MockBehavior.Strict);
    private readonly RecordingLogger _logger = new();
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        _client.Setup(c => c.ConnectAsync("smtp.example.test", 587, SecureSocketOptions.StartTls,
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _client.Setup(c => c.AuthenticateAsync("sender@example.test", "test-only-secret",
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), null))
            .ReturnsAsync("250 Accepted");
        _client.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _client.Setup(c => c.Dispose());

        _service = new EmailService(new EmailSettings
        {
            Host = "smtp.example.test",
            Port = 587,
            Usuario = "sender@example.test",
            Senha = "test-only-secret",
            Remetente = "sender@example.test",
            NomeRemetente = "Sender"
        }, new AssinaturaSettings(), new EmailContentSanitizer(), _logger, () => _client.Object);
    }

    [Fact]
    public async Task SuccessProducesSanitizedMultipartMessageAndReleasesConnection()
    {
        MimeMessage? sentMessage = null;
        _client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), null))
            .Callback<MimeMessage, CancellationToken, MailKit.ITransferProgress?>((message, _, _) => sentMessage = message)
            .ReturnsAsync("250 Accepted");

        await _service.SendEmailAsync(Message());

        Assert.NotNull(sentMessage);
        Assert.Contains("Hello", sentMessage.TextBody);
        Assert.Contains("<strong>Hello</strong>", sentMessage.HtmlBody);
        Assert.DoesNotContain("onclick", sentMessage.HtmlBody);
        Assert.IsType<MultipartAlternative>(sentMessage.Body);
        _client.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
        _client.Verify(c => c.Dispose(), Times.Once);
        Assert.Empty(_logger.Entries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcceptedMessageRemainsSuccessfulWhenDisconnectFails(bool cancellation)
    {
        Exception error = cancellation
            ? new OperationCanceledException("test-only-secret")
            : new IOException("test-only-secret");
        _client.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>())).ThrowsAsync(error);
        var envioService = new EnvioService(_service, TimeSpan.Zero);

        var result = await envioService.EnviarAsync(
            [new Destinatario { Email = "recipient@example.test", Nome = "Recipient" }],
            "Subject", "<p>Hello</p>");

        Assert.Equal(1, result.Enviados);
        Assert.Equal(0, result.Falhas);
        Assert.Empty(result.DetalhesFalhas);
        _client.Verify(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), null), Times.Once);
        _client.Verify(c => c.Dispose(), Times.Once);
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("test-only-secret", entry.Message);
    }

    [Theory]
    [InlineData("connect")]
    [InlineData("authenticate")]
    [InlineData("send")]
    public async Task FailureBeforeAcceptancePropagatesAndReleasesConnection(string stage)
    {
        var error = new IOException("Failure before confirmed acceptance");
        if (stage == "connect")
            _client.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(error);
        else if (stage == "authenticate")
            _client.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>())).ThrowsAsync(error);
        else
            _client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), null))
                .ThrowsAsync(error);

        Assert.Same(error, await Assert.ThrowsAsync<IOException>(() => _service.SendEmailAsync(Message())));

        _client.Verify(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Never);
        _client.Verify(c => c.Dispose(), Times.Once);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task SendCancellationPropagatesToCaller()
    {
        using var cancellation = new CancellationTokenSource();
        _client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), cancellation.Token, null))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var error = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.SendEmailAsync(Message(), cancellation.Token));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        _client.Verify(c => c.Dispose(), Times.Once);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task CancellationAfterAcceptanceDoesNotPreventRecordingSuccess()
    {
        using var cancellation = new CancellationTokenSource();
        _client.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), cancellation.Token, null))
            .Callback(() => cancellation.Cancel())
            .ReturnsAsync("250 Accepted");
        _client.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .Returns<bool, CancellationToken>((_, token) => Task.FromCanceled(token));

        await _service.SendEmailAsync(Message(), cancellation.Token);

        _client.Verify(c => c.Dispose(), Times.Once);
        Assert.Single(_logger.Entries);
    }

    private static EmailMessage Message() => new()
    {
        Destinatario = "recipient@example.test",
        DestinatarioNome = "Recipient",
        Assunto = "Subject",
        Body = "<p onclick=\"alert(1)\"><strong>Hello</strong></p>"
    };

    private sealed class RecordingLogger : ILogger<EmailService>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
