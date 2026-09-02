using EmailSender.Core.Models;

namespace EmailSender.Api.Jobs;

public class EnvioJob
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public StatusOperacaoEnvio Status {get; set;} = StatusOperacaoEnvio.Pendente;
    public StatusEnvio? EtapaAtual {get; set;}
    public int Total {get; set;}
    public int Processados {get; set;}
    public int Enviados {get; set;}
    public int Falhas {get; set;}
    public int Percentual => Total == 0 ? 0 : (int)Math.Round(Processados * 100d / Total);
    public int? SegundosRestantes {get; set;}
    public string? DestinatarioAtual {get; set;}
    public List<FalhaEnvio> DetalhesFalhas {get; set;} = new();
    public string? Erro {get; set;}
    public DateTime CriadoEm {get; init;} = DateTime.UtcNow;
    public DateTime? IniciadoEm {get; set;}
    public DateTime? FinalizadoEm {get; set;}
}