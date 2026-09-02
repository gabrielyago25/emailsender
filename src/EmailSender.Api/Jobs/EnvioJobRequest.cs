using EmailSender.Core.Models;

namespace EmailSender.Api.Jobs;

public class EnvioJobRequest
{
    public Guid JobId {get; init;}
    public List<Destinatario> Destinatarios {get; init;} = new();
    public string Assunto {get; init;} = string.Empty;
    public string Corpo {get; init;} = string.Empty;
}