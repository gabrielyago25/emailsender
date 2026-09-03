namespace EmailSender.Core.Models;

public class DestinatarioInvalido
{
    public int Linha {get; set;}
    public string Nome {get; set;} = string.Empty;
    public string Email {get; set;} = string.Empty;
    public string Motivo {get; set;} = string.Empty;
}