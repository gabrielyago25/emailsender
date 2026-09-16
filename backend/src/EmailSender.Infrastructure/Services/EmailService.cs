using EmailSender.Infrastructure.Configuration;
using EmailSender.Core.Models;
using EmailSender.Core.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using System.Net;
using Microsoft.Extensions.Logging;

namespace EmailSender.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly AssinaturaSettings _assinaturaSettings;
    private readonly IEmailContentSanitizer _contentSanitizer;
    private readonly ILogger<EmailService> _logger;
    private readonly Func<ISmtpClient> _createClient;
    public EmailService(
        EmailSettings settings,
        AssinaturaSettings assinaturaSettings,
        IEmailContentSanitizer contentSanitizer,
        ILogger<EmailService> logger)
        : this(settings, assinaturaSettings, contentSanitizer, logger, () => new SmtpClient())
    {
    }

    internal EmailService(
        EmailSettings settings,
        AssinaturaSettings assinaturaSettings,
        IEmailContentSanitizer contentSanitizer,
        ILogger<EmailService> logger,
        Func<ISmtpClient> createClient)
    {
        _settings = settings;
        _assinaturaSettings = assinaturaSettings;
        _contentSanitizer = contentSanitizer;
        _logger = logger;
        _createClient = createClient;
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

        using var client = _createClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.Usuario, _settings.Senha, cancellationToken);
        await client.SendAsync(message, cancellationToken);

        // SendAsync concluído significa aceitação pelo SMTP. Uma falha no QUIT
        // não pode transformar essa aceitação em falha e incentivar reenvio.
        using var disconnectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        disconnectTimeout.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await client.DisconnectAsync(true, disconnectTimeout.Token);
        }
        catch (Exception)
        {
            // Não registrar a exceção SMTP: sua mensagem pode conter dados sensíveis.
            // O using ainda libera a conexão quando a desconexão graciosa falha.
            _logger.LogWarning("A mensagem foi aceita pelo SMTP, mas a desconexão não foi concluída normalmente.");
        }
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
