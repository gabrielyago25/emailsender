using emailsender.app.Models;

namespace emailsender.app.Services;

public class EnvioService
{
    private readonly EmailService _emailService;

    public EnvioService(EmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<ResultadoEnvio> EnviarAsync(
        List<Destinatario> destinatarios,
        string assunto,
        string corpo)
    {
        var resultado = new ResultadoEnvio
        {
            Total = destinatarios.Count
        };

        foreach (var destinatario in destinatarios)
        {
            var emailMessage = new EmailMessage
            {
                Destinatario = destinatario.Email,
                DestinatarioNome = destinatario.Nome,
                Assunto = assunto,
                Body = corpo
            };

            try
            {
                await _emailService.SendEmail(emailMessage);

                resultado.Enviados++;
            }
            catch (Exception ex)
            {
                resultado.Falhas++;

                resultado.DetalhesFalhas.Add(
                    new FalhaEnvio
                    {
                        Nome = destinatario.Nome,
                        Email = destinatario.Email,
                        Erro = ex.Message
                    }
                );
            }
        }

        return resultado;
    }
}