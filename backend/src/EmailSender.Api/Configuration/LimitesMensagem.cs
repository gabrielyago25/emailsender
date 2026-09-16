namespace EmailSender.Api.Configuration;

public sealed class LimitesMensagem
{
    public const string Secao = "LimitesMensagem";

    public int AssuntoMaximoCaracteres { get; set; } = 200;
    public int CorpoMaximoBytes { get; set; } = 100 * 1024;
}
