namespace emailsender.app.Models;

public class ResultadoEnvio
{
    public int Total { get; set; }
    public int Enviados { get; set; }
    public int Falhas { get; set; }

    public List<FalhaEnvio> DetalhesFalhas { get; set; } = new();
}