namespace emailsender.app.Models;

public class ProgressoEnvio
{
    public int Processados {get; set;}
    public int Total {get; set;}
    public string? Nome {get; set;}
    public string? Email {get; set;}
    public string? Status {get; set;}
    public int? SegundosRestantes {get; set;}

    public int Percentual => Total == 0 ? 0 : (int)Math.Round(Processados * 100d / Total);
}