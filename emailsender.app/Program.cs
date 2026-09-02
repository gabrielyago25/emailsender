using emailsender.app.Config;
using emailsender.app.Models;
using emailsender.app.Services;
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
var excelService = new ExcelService();
var envioService = new EnvioService(emailService);

Console.WriteLine("==================================");
Console.WriteLine("          EMAIL SENDER");
Console.WriteLine("==================================");
Console.WriteLine();

Console.Write("Informe o caminho da planilha XLSX: ");

var caminhoPlanilha = Console.ReadLine();

if (string.IsNullOrWhiteSpace(caminhoPlanilha))
{
    Console.WriteLine("Nenhum caminho foi informado.");
    return;
}

caminhoPlanilha = caminhoPlanilha.Trim().Trim('"');

if (!File.Exists(caminhoPlanilha))
{
    Console.WriteLine("Planilha não encontrada.");
    return;
}

List<Destinatario> destinatarios;

try
{
    destinatarios = excelService.LerDestinatarios(caminhoPlanilha);
}
catch (Exception ex)
{
    Console.WriteLine("Erro ao ler a planilha:");
    Console.WriteLine(ex.Message);
    return;
}

if (destinatarios.Count == 0)
{
    Console.WriteLine("Nenhum destinatário encontrado.");
    return;
}

Console.WriteLine();
Console.WriteLine($"Destinatários encontrados: {destinatarios.Count}");
Console.WriteLine("----------------------------------");

foreach (var destinatario in destinatarios)
{
    Console.WriteLine($"{destinatario.Nome} - {destinatario.Email}");
}

Console.WriteLine("----------------------------------");
Console.WriteLine();

Console.Write("Informe o assunto do e-mail: ");
var assunto = Console.ReadLine();

if (string.IsNullOrWhiteSpace(assunto))
{
    Console.WriteLine("O assunto não pode ficar vazio.");
    return;
}

Console.WriteLine();
Console.WriteLine("Digite a mensagem do e-mail.");
Console.WriteLine("Digite FIM em uma nova linha para finalizar:");
Console.WriteLine();

var linhasMensagem = new List<string>();

while (true)
{
    var linha = Console.ReadLine();

    if (linha?.Trim().Equals(
            "FIM",
            StringComparison.OrdinalIgnoreCase) == true)
    {
        break;
    }

    linhasMensagem.Add(linha ?? string.Empty);
}

var corpo = string.Join(
    Environment.NewLine,
    linhasMensagem
);

if (string.IsNullOrWhiteSpace(corpo))
{
    Console.WriteLine("A mensagem não pode ficar vazia.");
    return;
}

Console.WriteLine();
Console.WriteLine("==================================");
Console.WriteLine("        CONFIRMAÇÃO DO ENVIO");
Console.WriteLine("==================================");
Console.WriteLine($"Destinatários: {destinatarios.Count}");
Console.WriteLine($"Assunto: {assunto}");
Console.WriteLine();

Console.Write("Deseja realizar o envio? (S/N): ");

var confirmacao = Console.ReadLine();

if (!string.Equals(
        confirmacao,
        "S",
        StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Envio cancelado.");
    return;
}

Console.WriteLine();
Console.WriteLine("Realizando envios...");
Console.WriteLine();

var resultado = await envioService.EnviarAsync(destinatarios, assunto, corpo);

Console.WriteLine("==================================");
Console.WriteLine("          RESULTADO");
Console.WriteLine("==================================");
Console.WriteLine($"Enviados com sucesso: {resultado.Enviados}");
Console.WriteLine($"Falhas: {resultado.Falhas}");
Console.WriteLine($"Total: {resultado.Total}");

if (resultado.DetalhesFalhas.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Detalhes das falhas:");

    foreach (var falha in resultado.DetalhesFalhas)
    {
        Console.WriteLine($"Destinatário: {falha.Nome}");
        Console.WriteLine($"E-mail: {falha.Email}");
        Console.WriteLine($"Erro: {falha.Erro}");
    }
}