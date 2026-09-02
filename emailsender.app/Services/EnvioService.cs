using emailsender.app.Models;

namespace emailsender.app.Services;

public class EnvioService
{
    private readonly EmailService _emailService;
    private readonly TimeSpan _intervaloEntreEnvios;

    public EnvioService(EmailService emailService, TimeSpan? intervaloEntreEnvios = null)
    {
        _emailService = emailService;
        _intervaloEntreEnvios = intervaloEntreEnvios ?? TimeSpan.FromSeconds(60);
    }

    public async Task<ResultadoEnvio> EnviarAsync(
        List<Destinatario> destinatarios,
        string assunto,
        string corpo,
        CancellationToken cancellationToken = default)
    {
        var resultado = new ResultadoEnvio
        {
            Total = destinatarios.Count
        };

        for (var i = 0; i < destinatarios.Count; i++)
        {
            var destinatario = destinatarios[i];
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

            // Aguarda apenas se existir outro destinatário
            if (i < destinatarios.Count - 1)
            {
                await Task.Delay(_intervaloEntreEnvios, cancellationToken);
            }
        }

        return resultado;
    }
}