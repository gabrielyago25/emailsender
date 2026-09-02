using Microsoft.Extensions.Configuration;

namespace emailsender.app.Config;

public static class ConfigurationLoader
{
    public static EmailSettings Carregar()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<EmailSettings>()
            .Build();

        var emailSettings = configuration
            .GetSection("EmailSettings")
            .Get<EmailSettings>();

        if (emailSettings is null)
        {
            throw new InvalidOperationException(
                "Não foi possível carregar as configurações de e-mail."
            );
        }

        if (string.IsNullOrWhiteSpace(emailSettings.Host) ||
            string.IsNullOrWhiteSpace(emailSettings.Usuario) ||
            string.IsNullOrWhiteSpace(emailSettings.Senha))
        {
            throw new InvalidOperationException(
                "As configurações SMTP estão incompletas."
            );
        }

        return emailSettings;
    }
}