namespace EmailSender.Core.Models;

public class EmailMessage{
    public string Destinatario {get; set;} = string.Empty;
    public string DestinatarioNome {get; set;} = string.Empty; 

    public string Assunto {get; set;} = string.Empty;
    public string Body {get; set;} = string.Empty;
}