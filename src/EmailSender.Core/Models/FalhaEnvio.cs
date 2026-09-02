namespace EmailSender.Core.Models;

public class FalhaEnvio
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Erro { get; set; } = string.Empty;
}