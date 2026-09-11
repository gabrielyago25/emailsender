namespace EmailSender.Infrastructure.Configuration;

public class AssinaturaSettings
{
    public bool Habilitada { get; set; }
    public string Html { get; set; } = string.Empty;
    public string CaminhoImagem { get; set; } = string.Empty;
    public string TextoAlternativoImagem { get; set; } = "Assinatura";
    public int LarguraMaximaImagem { get; set; } = 480;
}
