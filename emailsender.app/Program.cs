using emailsender.app.Config;
using emailsender.app.Presentation;
using emailsender.app.Services;

try
{
    var emailSettings = ConfigurationLoader.Carregar();

    var emailService = new EmailService(emailSettings);
    var excelService = new ExcelService();
    var envioService = new EnvioService(emailService);

    var app = new ConsoleApplication(
        excelService,
        envioService
    );

    await app.ExecutarAsync();
}
catch (Exception ex)
{
    Console.WriteLine("Erro ao iniciar a aplicação:");
    Console.WriteLine(ex.Message);
}