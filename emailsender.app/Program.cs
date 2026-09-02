using emailsender.app.Config;
using emailsender.app.Services;
using emailsender.app.Models;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

var emailSettings = configuration
    .GetSection("EmailSettings")
    .Get<EmailSettings>();

if (emailSettings is null)
{
    Console.WriteLine("Não foi possível carregar as configurações de e-mail.");
    return;
}

if (string.IsNullOrWhiteSpace(emailSettings.Host) ||
    string.IsNullOrWhiteSpace(emailSettings.Usuario) ||
    string.IsNullOrWhiteSpace(emailSettings.Senha))
{
    Console.WriteLine("As configurações SMTP estão incompletas.");
    return;
}

var emailService = new EmailService(emailSettings);

var emailMessage = new EmailMessage {
    Destinatario = "", // Email do destinatário
    DestinatarioNome = "", // Nome do destinatário
    Assunto = "Teste de envio de e-mail",
    Body = @"Olá!
    
    Testando envio de e-mail usando C#.
    
    MailKit funcionando."};

    try {
        Console.WriteLine("Enviando e-mail...");

        await emailService.SendEmail(emailMessage);

        Console.WriteLine("E-mail enviado com sucesso!");
    } catch (Exception ex) {
        Console.WriteLine("Erro ao enviar e-mail:");
        Console.WriteLine(ex.Message);
    }
