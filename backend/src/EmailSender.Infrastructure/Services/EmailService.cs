using EmailSender.Infrastructure.Configuration;
using EmailSender.Core.Models;
using EmailSender.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EmailSender.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(EmailSettings settings)
    {
        _settings = settings;
    }

    public async Task SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default){

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_settings.NomeRemetente, _settings.Remetente));
        message.To.Add(new MailboxAddress(emailMessage.DestinatarioNome, emailMessage.Destinatario));
        message.Subject = emailMessage.Assunto;
        var bodyBuilder = new BodyBuilder {HtmlBody = emailMessage.Body};
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.Usuario, _settings.Senha, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}