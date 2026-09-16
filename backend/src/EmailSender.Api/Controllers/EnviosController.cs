using EmailSender.Api.Jobs;
using EmailSender.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using EmailSender.Api.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

namespace EmailSender.Api.Controllers;

[ApiController]
[Route("api/envios")]
public class EnviosController : ControllerBase
{
    private readonly IExcelService _excelService;
    private readonly EnvioJobStore _jobStore;
    private readonly EnvioJobQueue _jobQueue;
    private readonly IEmailContentSanitizer _contentSanitizer;
    private readonly LimitesMensagem _limites;

    public EnviosController(
        IExcelService excelService,
        EnvioJobStore jobStore,
        EnvioJobQueue jobQueue,
        IEmailContentSanitizer contentSanitizer,
        IOptions<LimitesMensagem> limites)
    {
        _excelService = excelService;
        _jobStore = jobStore;
        _jobQueue = jobQueue;
        _contentSanitizer = contentSanitizer;
        _limites = limites.Value;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Criar(
        [FromForm] IFormFile arquivo,
        [FromForm] string assunto,
        [FromForm] string corpo,
        CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new
            {
                mensagem = "Nenhuma planilha foi enviada."
            });
        }

        if (string.IsNullOrWhiteSpace(assunto))
        {
            return BadRequest(new
            {
                mensagem = "O assunto é obrigatório."
            });
        }

        if (string.IsNullOrWhiteSpace(corpo))
        {
            return BadRequest(new
            {
                mensagem = "O corpo do e-mail é obrigatório."
            });
        }

        if (assunto.Length > _limites.AssuntoMaximoCaracteres)
        {
            return BadRequest(new { mensagem = $"O assunto não pode ultrapassar {_limites.AssuntoMaximoCaracteres} caracteres." });
        }

        if (assunto.Any(char.IsControl))
        {
            return BadRequest(new { mensagem = "O assunto não pode conter quebras de linha ou caracteres de controle." });
        }

        // Verifica a entrada antes de executar o parser HTML ou ler a planilha.
        if (CorpoExcedeLimite(corpo))
        {
            return BadRequest(new { mensagem = $"O corpo do e-mail não pode ultrapassar {_limites.CorpoMaximoBytes} bytes em UTF-8." });
        }

        var corpoSanitizado = _contentSanitizer.Sanitizar(corpo).Trim();

        if (CorpoExcedeLimite(corpoSanitizado))
        {
            return BadRequest(new { mensagem = $"O corpo sanitizado não pode ultrapassar {_limites.CorpoMaximoBytes} bytes em UTF-8." });
        }

        if (!_contentSanitizer.PossuiTexto(corpoSanitizado))
        {
            return BadRequest(new { mensagem = "O corpo do e-mail deve conter texto após a remoção de conteúdo não permitido." });
        }

        var extensao = Path.GetExtension(arquivo.FileName);

        if (!string.Equals(
                extensao,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                mensagem = "O arquivo deve estar no formato XLSX."
            });
        }

        const long tamanhoMaximo = 5 * 1024 * 1024;

        if (arquivo.Length > tamanhoMaximo)
        {
            return BadRequest(new
            {
                mensagem = "A planilha não pode ultrapassar 5 MB."
            });
        }

        List<EmailSender.Core.Models.Destinatario> destinatarios;

        try
        {
            using var stream = arquivo.OpenReadStream();

            var resultado = _excelService.LerDestinatarios(stream);
            destinatarios = resultado.DestinatariosValidos;
        }
        catch
        {
            return BadRequest(new
            {
                mensagem = "Não foi possível processar a planilha."
            });
        }

        if (destinatarios.Count == 0)
        {
            return BadRequest(new
            {
                mensagem = "Nenhum destinatário válido foi encontrado."
            });
        }

        var job = _jobStore.Criar(
            destinatarios.Count
        );

        var request = new EnvioJobRequest
        {
            JobId = job.Id,
            Destinatarios = destinatarios,
            Assunto = assunto.Trim(),
            Corpo = corpoSanitizado
        };

        await _jobQueue.EnfileirarAsync(
            request,
            cancellationToken
        );

        return Accepted(new
        {
            id = job.Id,
            status = job.Status,
            total = job.Total
        });
    }

    [HttpGet("{id:guid}")]
    public IActionResult Obter(Guid id)
    {
        var job = _jobStore.Obter(id);

        if (job is null)
        {
            return NotFound(new
            {
                mensagem = "Envio não encontrado."
            });
        }

        return Ok(job);
    }

    private bool CorpoExcedeLimite(string corpo) =>
        corpo.Length > _limites.CorpoMaximoBytes ||
        Encoding.UTF8.GetByteCount(corpo) > _limites.CorpoMaximoBytes;
}
