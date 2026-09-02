using EmailSender.Core.Models;
using EmailSender.Core.Interfaces;

namespace EmailSender.Core.Services;

public class EnvioService
{
    private readonly IEmailService _emailService;
    private readonly TimeSpan _intervaloEntreEnvios;

    public EnvioService(IEmailService emailService, TimeSpan? intervaloEntreEnvios = null)
    {
        _emailService = emailService;

        _intervaloEntreEnvios = intervaloEntreEnvios ?? TimeSpan.FromSeconds(60);
    }

    public async Task<ResultadoEnvio> EnviarAsync(
        List<Destinatario> destinatarios,
        string assunto,
        string corpo,
        IProgress<ProgressoEnvio>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = new ResultadoEnvio
        {
            Total = destinatarios.Count
        };

        for (var i = 0; i < destinatarios.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinatario = destinatarios[i];

            progresso?.Report(new ProgressoEnvio
            {
                Processados = i,
                Total = destinatarios.Count,
                Nome = destinatario.Nome,
                Email = destinatario.Email,
                Status = StatusEnvio.Enviando
            });

            var emailMessage = new EmailMessage
            {
                Destinatario = destinatario.Email,
                DestinatarioNome = destinatario.Nome,
                Assunto = assunto,
                Body = corpo
            };

            try
            {
                await _emailService.SendEmailAsync(emailMessage, cancellationToken);

                resultado.Enviados++;

                progresso?.Report(new ProgressoEnvio
                {
                    Processados = i + 1,
                    Total = destinatarios.Count,
                    Nome = destinatario.Nome,
                    Email = destinatario.Email,
                    Status = StatusEnvio.Enviado
                });
            }
            catch (OperationCanceledException)
            {
                throw;
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

                progresso?.Report(new ProgressoEnvio
                {
                    Processados = i + 1,
                    Total = destinatarios.Count,
                    Nome = destinatario.Nome,
                    Email = destinatario.Email,
                    Status = StatusEnvio.Falha
                });
            }

            // Aguarda somente se ainda houver outro destinatário.
            if (i < destinatarios.Count - 1)
            {
                await AguardarProximoEnvioAsync(
                    i + 1,
                    destinatarios.Count,
                    destinatario,
                    progresso,
                    cancellationToken
                );
            }
        }

        return resultado;
    }

    private async Task AguardarProximoEnvioAsync(
        int processados,
        int total,
        Destinatario destinatario,
        IProgress<ProgressoEnvio>? progresso,
        CancellationToken cancellationToken)
    {
        var segundos =
            (int)Math.Ceiling(_intervaloEntreEnvios.TotalSeconds);

        for (var restante = segundos; restante > 0; restante--)
        {
            progresso?.Report(new ProgressoEnvio
            {
                Processados = processados,
                Total = total,
                Nome = destinatario.Nome,
                Email = destinatario.Email,
                Status = StatusEnvio.Aguardando,
                SegundosRestantes = restante
            });

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken
            );
        }
    }
}