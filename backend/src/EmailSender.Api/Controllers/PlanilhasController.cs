using EmailSender.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EmailSender.Api.Controllers;

[ApiController]
[Route("api/planilhas")]
public class PlanilhasController : ControllerBase
{
    private readonly IExcelService _excelService;
    private readonly ILogger<PlanilhasController> _logger;

    public PlanilhasController(
        IExcelService excelService,
        ILogger<PlanilhasController> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    [HttpPost("validar")]
    [Consumes("multipart/form-data")]
    public IActionResult Validar(
        [FromForm] IFormFile arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            return BadRequest(new {mensagem = "Nenhuma planilha foi enviada."});
        }

        var extensao = Path.GetExtension(arquivo.FileName);

        if (!string.Equals(extensao, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new {mensagem = "O arquivo deve estar no formato XLSX."});
        }

        const long tamanhoMaximo = 5 * 1024 * 1024;

        if (arquivo.Length > tamanhoMaximo)
        {
            return BadRequest(new {mensagem = "A planilha não pode ultrapassar 5 MB."});
        }

        try
        {
            using var stream = arquivo.OpenReadStream();

            var destinatarios = _excelService.LerDestinatarios(stream);

            return Ok(new
            {
                total = destinatarios.Count,
                destinatarios
            });
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar a planilha {Arquivo}", arquivo.FileName);

            return BadRequest(new {mensagem = "Não foi possível processar a planilha."});
        }
    }
}