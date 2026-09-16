using System.Text.Json;
using EmailSender.Api.Jobs;
using EmailSender.Core.Interfaces;
using EmailSender.Core.Models;
using EmailSender.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace EmailSender.Api.Tests;

public class EnvioBackgroundServiceTests
{
    private const string SensitiveDetail = "smtp-password=test-secret; internal-host=private.example.test";

    [Fact]
    public async Task RecipientFailureUsesSafeMessageAndLogsOnlyIdentifiersAndType()
    {
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException(SensitiveDetail));
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scope = Scope(new EnvioService(email.Object, TimeSpan.Zero), done);
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var store = new EnvioJobStore();
        var queue = new EnvioJobQueue();
        var logger = new RecordingLogger();
        var job = await Enqueue(store, queue);
        using var worker = new EnvioBackgroundService(queue, store, factory.Object, logger);

        await worker.StartAsync(default);
        try { await done.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { await Stop(worker); }

        Assert.Equal(StatusOperacaoEnvio.Concluido, job.Status);
        Assert.Equal(1, job.Falhas);
        Assert.Equal(0, job.Enviados);
        Assert.Contains("antes de reenviar", Assert.Single(job.DetalhesFalhas).Erro);
        Assert.DoesNotContain(SensitiveDetail, JsonSerializer.Serialize(job));
        AssertCleared(job);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains(job.Id.ToString(), entry.Message);
        Assert.Contains(nameof(IOException), entry.Message);
        Assert.DoesNotContain("recipient@example.test", entry.Message);
        Assert.DoesNotContain(SensitiveDetail, entry.Message);
        Assert.Null(entry.Exception);
        email.Verify(e => e.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnexpectedFailureIsSafeAndDoesNotPreventNextJob()
    {
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scope = Scope(new EnvioService(email.Object, TimeSpan.Zero), done);
        var factory = new Mock<IServiceScopeFactory>();
        factory.SetupSequence(f => f.CreateScope()).Throws(new InvalidOperationException(SensitiveDetail))
            .Returns(scope.Object);
        var store = new EnvioJobStore();
        var queue = new EnvioJobQueue();
        var first = await Enqueue(store, queue);
        var second = await Enqueue(store, queue);
        var logger = new RecordingLogger();
        using var worker = new EnvioBackgroundService(queue, store, factory.Object, logger);

        await worker.StartAsync(default);
        try { await done.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { await Stop(worker); }

        Assert.Equal(StatusOperacaoEnvio.Falhou, first.Status);
        Assert.Contains("falha interna", first.Erro);
        Assert.DoesNotContain(SensitiveDetail, JsonSerializer.Serialize(first));
        AssertCleared(first);
        Assert.Equal(StatusOperacaoEnvio.Concluido, second.Status);
        Assert.Equal(1, second.Enviados);
        AssertCleared(second);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(first.Id.ToString(), entry.Message);
        Assert.Contains(nameof(InvalidOperationException), entry.Message);
        Assert.DoesNotContain(SensitiveDetail, entry.Message);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public async Task ShutdownClearsTransientProgressWithoutClaimingSuccess()
    {
        var sending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns<EmailMessage, CancellationToken>((_, token) =>
            {
                sending.TrySetResult();
                return Task.Delay(Timeout.Infinite, token);
            });
        var scope = Scope(new EnvioService(email.Object, TimeSpan.Zero), new TaskCompletionSource());
        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        var store = new EnvioJobStore();
        var queue = new EnvioJobQueue();
        var job = await Enqueue(store, queue);
        var logger = new RecordingLogger();
        using var worker = new EnvioBackgroundService(queue, store, factory.Object, logger);

        await worker.StartAsync(default);
        try { await sending.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { await Stop(worker); }

        Assert.Equal(StatusOperacaoEnvio.Cancelado, job.Status);
        Assert.Equal(0, job.Enviados);
        Assert.Equal(0, job.Processados);
        Assert.Null(job.Erro);
        AssertCleared(job);
    }

    private static Mock<IServiceScope> Scope(EnvioService service, TaskCompletionSource done)
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(EnvioService))).Returns(service);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object);
        scope.Setup(s => s.Dispose()).Callback(() => done.TrySetResult());
        return scope;
    }

    private static async Task<EnvioJob> Enqueue(EnvioJobStore store, EnvioJobQueue queue)
    {
        var job = store.Criar(1);
        await queue.EnfileirarAsync(new EnvioJobRequest
        {
            JobId = job.Id,
            Assunto = "Assunto",
            Corpo = "<p>Texto</p>",
            Destinatarios = [new Destinatario { Email = "recipient@example.test", Nome = "Teste" }]
        });
        return job;
    }

    private static async Task Stop(EnvioBackgroundService worker)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StopAsync(timeout.Token);
    }

    private static void AssertCleared(EnvioJob job)
    {
        Assert.Null(job.EtapaAtual);
        Assert.Null(job.SegundosRestantes);
        Assert.Null(job.DestinatarioAtual);
        Assert.NotNull(job.FinalizadoEm);
    }

    private sealed class RecordingLogger : ILogger<EnvioBackgroundService>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
