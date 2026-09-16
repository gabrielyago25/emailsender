using System.Text;
using EmailSender.Api.Configuration;
using EmailSender.Api.Controllers;
using EmailSender.Api.Jobs;
using EmailSender.Core.Interfaces;
using EmailSender.Core.Models;
using EmailSender.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace EmailSender.Api.Tests;

public class EnviosControllerTests
{
    private readonly Mock<IExcelService> _excel = new(MockBehavior.Strict);
    private readonly EnvioJobStore _store = new();
    private readonly EnvioJobQueue _queue = new();

    private EnviosController Controller(LimitesMensagem? limites = null, IEmailContentSanitizer? sanitizer = null) =>
        new(_excel.Object, _store, _queue, sanitizer ?? new EmailContentSanitizer(),
            Options.Create(limites ?? new LimitesMensagem()));

    private static FormFile Planilha() => new(new MemoryStream([1]), 0, 1, "arquivo", "destinatarios.xlsx");

    private void AllowExcel() => _excel.Setup(e => e.LerDestinatarios(It.IsAny<Stream>()))
        .Returns(new ResultadoLeituraPlanilha
        {
            DestinatariosValidos = [new Destinatario { Nome = "Teste", Email = "recipient@example.test" }]
        });

    private async Task AssertRejected(IActionResult result)
    {
        Assert.IsType<BadRequestObjectResult>(result);
        _excel.Verify(e => e.LerDestinatarios(It.IsAny<Stream>()), Times.Never);
        // Uma fila vazia deve permanecer sem trabalho disponível.
        using var cancellation = new CancellationTokenSource();
        var next = _queue.ObterProximoAsync(cancellation.Token).AsTask();
        Assert.False(next.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => next);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p><br></p>")]
    [InlineData("<p>&nbsp; &#8203; &#xfeff;</p>")]
    [InlineData("<ul><li></li><li><br></li></ul>")]
    [InlineData("<script>alert('blocked')</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    public async Task RejectsBodyWithoutTextBeforeReadingSpreadsheet(string body)
    {
        await AssertRejected(await Controller().Criar(Planilha(), "Assunto", body, default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Assunto\r\nBcc: hidden@example.test")]
    [InlineData("Assunto\tTeste")]
    [InlineData("Assunto\0Teste")]
    public async Task RejectsMissingOrControlCharacterSubject(string subject)
    {
        await AssertRejected(await Controller().Criar(Planilha(), subject, "<p>Texto</p>", default));
    }

    [Fact]
    public async Task RejectsSubjectAboveFrontendLimit()
    {
        await AssertRejected(await Controller().Criar(Planilha(), new string('a', 201), "Texto", default));
    }

    [Fact]
    public async Task RejectsOversizedBodyBeforeSanitizing()
    {
        var sanitizer = new Mock<IEmailContentSanitizer>(MockBehavior.Strict);

        await AssertRejected(await Controller(sanitizer: sanitizer.Object)
            .Criar(Planilha(), "Assunto", new string('a', 100 * 1024 + 1), default));

        sanitizer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MeasuresUtf8BytesInsteadOfCharacterCount()
    {
        var limits = new LimitesMensagem { CorpoMaximoBytes = 10 };
        const string body = "áááááá";
        Assert.True(body.Length < limits.CorpoMaximoBytes);

        await AssertRejected(await Controller(limits).Criar(Planilha(), "Assunto", body, default));
    }

    [Fact]
    public async Task ChecksSizeAfterSanitizerExpansion()
    {
        // Um sanitizador pode normalizar entidades e expandir o HTML recebido.
        var sanitizer = new Mock<IEmailContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(s => s.Sanitizar("Texto")).Returns(new string('a', 11));

        await AssertRejected(await Controller(new LimitesMensagem { CorpoMaximoBytes = 10 }, sanitizer.Object)
            .Criar(Planilha(), "Assunto", "Texto", default));

        sanitizer.Verify(s => s.PossuiTexto(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HonorsConfiguredSubjectLimit()
    {
        await AssertRejected(await Controller(new LimitesMensagem { AssuntoMaximoCaracteres = 5 })
            .Criar(Planilha(), "Seis!!", "Texto", default));
    }

    [Fact]
    public async Task AcceptsExactSubjectAndUtf8BodyLimits()
    {
        AllowExcel();
        const string body = "<p>Olá 😀</p>";
        var limits = new LimitesMensagem { CorpoMaximoBytes = Encoding.UTF8.GetByteCount(body) };
        var subject = new string('a', limits.AssuntoMaximoCaracteres);

        var result = await Controller(limits).Criar(Planilha(), subject, body, default);

        Assert.IsType<AcceptedResult>(result);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var request = await _queue.ObterProximoAsync(deadline.Token);
        Assert.Equal(subject, request.Assunto);
        Assert.Equal(body, request.Corpo);
        Assert.NotNull(_store.Obter(request.JobId));
    }

    [Fact]
    public async Task QueuesSanitizedBodyWithAllowedFormatting()
    {
        AllowExcel();
        const string body = "<p onclick=\"alert(1)\"><strong>Olá</strong> <em>pessoal</em> <u>hoje</u></p>" +
            "<ul><li><span style=\"font-size:18px;color:red\">Item</span></li></ul><script>alert(1)</script>";

        Assert.IsType<AcceptedResult>(await Controller().Criar(Planilha(), " Assunto ", body, default));

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var request = await _queue.ObterProximoAsync(deadline.Token);
        Assert.Equal("Assunto", request.Assunto);
        Assert.Contains("<strong>Olá</strong>", request.Corpo);
        Assert.Contains("<em>pessoal</em>", request.Corpo);
        Assert.Contains("<u>hoje</u>", request.Corpo);
        Assert.Contains("<ul><li>", request.Corpo);
        Assert.Contains("font-size", request.Corpo);
        Assert.DoesNotContain("color", request.Corpo);
        Assert.DoesNotContain("onclick", request.Corpo);
        Assert.DoesNotContain("script", request.Corpo);
        Assert.DoesNotContain("alert", request.Corpo);
        _excel.Verify(e => e.LerDestinatarios(It.IsAny<Stream>()), Times.Once);
    }
}
