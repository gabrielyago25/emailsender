using EmailSender.Infrastructure.Configuration;
using EmailSender.Core.Models;
using EmailSender.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using System.Net;

namespace EmailSender.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly AssinaturaSettings _assinaturaSettings;
    private readonly IEmailContentSanitizer _contentSanitizer;
    public EmailService(
        EmailSettings settings,
        AssinaturaSettings assinaturaSettings,
        IEmailContentSanitizer contentSanitizer)
    {
        _settings = settings;
        _assinaturaSettings = assinaturaSettings;
        _contentSanitizer = contentSanitizer;
    }

    public async Task SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default){

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_settings.NomeRemetente, _settings.Remetente));
        message.To.Add(new MailboxAddress(emailMessage.DestinatarioNome, emailMessage.Destinatario));
        message.Subject = emailMessage.Assunto;
        var htmlSanitizado = _contentSanitizer.Sanitizar(emailMessage.Body);
        var bodyBuilder = new BodyBuilder();
        var assinaturaHtml = MontarAssinaturaHtml(bodyBuilder);
        var htmlCompleto = htmlSanitizado + assinaturaHtml;

        bodyBuilder.TextBody = _contentSanitizer.ConverterParaTexto(htmlCompleto);
        bodyBuilder.HtmlBody = htmlCompleto;
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.Usuario, _settings.Senha, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private string MontarAssinaturaHtml(BodyBuilder bodyBuilder)
    {
        if (!_assinaturaSettings.Habilitada)
        {
            return string.Empty;
        }

        var html = _assinaturaSettings.Html
            .Replace("{{NomeRemetente}}", WebUtility.HtmlEncode(_settings.NomeRemetente), StringComparison.OrdinalIgnoreCase)
            .Replace("{{Remetente}}", WebUtility.HtmlEncode(_settings.Remetente), StringComparison.OrdinalIgnoreCase);

        var imagemHtml = string.Empty;

        if (!string.IsNullOrWhiteSpace(_assinaturaSettings.CaminhoImagem))
        {
            var imagem = bodyBuilder.LinkedResources.Add(_assinaturaSettings.CaminhoImagem);
            imagem.ContentId = MimeUtils.GenerateMessageId();

            var textoAlternativo = WebUtility.HtmlEncode(_assinaturaSettings.TextoAlternativoImagem);
            var larguraMaxima = Math.Clamp(_assinaturaSettings.LarguraMaximaImagem, 1, 1200);

            imagemHtml = $"<img src=\"cid:{imagem.ContentId}\" alt=\"{textoAlternativo}\" style=\"display:block;max-width:{larguraMaxima}px;height:auto;\">";
        }

        if (html.Contains("{{Imagem}}", StringComparison.OrdinalIgnoreCase))
        {
            html = html.Replace("{{Imagem}}", imagemHtml, StringComparison.OrdinalIgnoreCase);
        }
        else if (!string.IsNullOrEmpty(imagemHtml))
        {
            html += imagemHtml;
        }

        return $"<div class=\"email-signature\">{html}</div>";
    }
}
