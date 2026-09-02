using emailsender.app.Models;
using emailsender.app.Config;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace emailsender.app.Services;

public class EmailService{
    private readonly EmailSettings _settings;

    public EmailService(EmailSettings settings){
        _settings = settings;
    }

    public async Task SendEmail(EmailMessage emailMessage){
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_settings.NomeRemetente, _settings.Remetente));
        message.To.Add(new MailboxAddress(emailMessage.DestinatarioNome, emailMessage.Destinatario));
        message.Subject = emailMessage.Assunto;
        message.Body = new TextPart("plain"){
            Text = emailMessage.Body
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.Usuario, _settings.Senha);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}