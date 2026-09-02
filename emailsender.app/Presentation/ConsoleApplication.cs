using emailsender.app.Models;
using emailsender.app.Services;

namespace emailsender.app.Presentation;

public class ConsoleApplication
{
    private readonly ExcelService _excelService;
    private readonly EnvioService _envioService;

    public ConsoleApplication(
        ExcelService excelService,
        EnvioService envioService)
    {
        _excelService = excelService;
        _envioService = envioService;
    }

    public async Task ExecutarAsync()
    {
        ExibirCabecalho();

        var caminhoPlanilha = SolicitarPlanilha();

        if (caminhoPlanilha is null)
        {
            return;
        }

        var destinatarios = CarregarDestinatarios(caminhoPlanilha);

        if (destinatarios is null || destinatarios.Count == 0)
        {
            return;
        }

        ExibirDestinatarios(destinatarios);

        var assunto = SolicitarAssunto();

        if (assunto is null)
        {
            return;
        }

        var corpo = SolicitarCorpo();

        if (corpo is null)
        {
            return;
        }

        if (!ConfirmarEnvio(destinatarios.Count, assunto))
        {
            Console.WriteLine("Envio cancelado.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Realizando envios...");
        Console.WriteLine();

        var resultado = await _envioService.EnviarAsync(
            destinatarios,
            assunto,
            corpo
        );

        ExibirResultado(resultado);
    }

    private static void ExibirCabecalho()
    {
        Console.WriteLine("==================================");
        Console.WriteLine("          EMAIL SENDER");
        Console.WriteLine("==================================");
        Console.WriteLine();
    }

    private static string? SolicitarPlanilha()
    {
        Console.Write("Informe o caminho da planilha XLSX: ");

        var caminho = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(caminho))
        {
            Console.WriteLine("Nenhum caminho foi informado.");
            return null;
        }

        caminho = caminho.Trim().Trim('"');

        if (!File.Exists(caminho))
        {
            Console.WriteLine("Planilha não encontrada.");
            return null;
        }

        return caminho;
    }

    private List<Destinatario>? CarregarDestinatarios(
        string caminhoPlanilha)
    {
        try
        {
            var destinatarios =
                _excelService.LerDestinatarios(caminhoPlanilha);

            if (destinatarios.Count == 0)
            {
                Console.WriteLine(
                    "Nenhum destinatário válido encontrado."
                );

                return null;
            }

            return destinatarios;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao ler a planilha:");
            Console.WriteLine(ex.Message);

            return null;
        }
    }

    private static void ExibirDestinatarios(
        List<Destinatario> destinatarios)
    {
        Console.WriteLine();
        Console.WriteLine(
            $"Destinatários encontrados: {destinatarios.Count}"
        );

        Console.WriteLine("----------------------------------");

        foreach (var destinatario in destinatarios)
        {
            Console.WriteLine(
                $"{destinatario.Nome} - {destinatario.Email}"
            );
        }

        Console.WriteLine("----------------------------------");
        Console.WriteLine();
    }

    private static string? SolicitarAssunto()
    {
        Console.Write("Informe o assunto do e-mail: ");

        var assunto = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(assunto))
        {
            Console.WriteLine(
                "O assunto não pode ficar vazio."
            );

            return null;
        }

        return assunto;
    }

    private static string? SolicitarCorpo()
    {
        Console.WriteLine();
        Console.WriteLine("Digite a mensagem do e-mail.");
        Console.WriteLine(
            "Digite FIM em uma nova linha para finalizar:"
        );

        Console.WriteLine();

        var linhas = new List<string>();

        while (true)
        {
            var linha = Console.ReadLine();

            if (linha?.Trim().Equals(
                    "FIM",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                break;
            }

            linhas.Add(linha ?? string.Empty);
        }

        var corpo = string.Join(
            Environment.NewLine,
            linhas
        );

        if (string.IsNullOrWhiteSpace(corpo))
        {
            Console.WriteLine(
                "A mensagem não pode ficar vazia."
            );

            return null;
        }

        return corpo;
    }

    private static bool ConfirmarEnvio(
        int quantidadeDestinatarios,
        string assunto)
    {
        Console.WriteLine();
        Console.WriteLine("==================================");
        Console.WriteLine("        CONFIRMAÇÃO DO ENVIO");
        Console.WriteLine("==================================");

        Console.WriteLine(
            $"Destinatários: {quantidadeDestinatarios}"
        );

        Console.WriteLine($"Assunto: {assunto}");
        Console.WriteLine();

        Console.Write(
            "Deseja realizar o envio? (S/N): "
        );

        var confirmacao = Console.ReadLine();

        return string.Equals(
            confirmacao,
            "S",
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static void ExibirResultado(
        ResultadoEnvio resultado)
    {
        Console.WriteLine();
        Console.WriteLine("==================================");
        Console.WriteLine("          RESULTADO");
        Console.WriteLine("==================================");

        Console.WriteLine(
            $"Enviados com sucesso: {resultado.Enviados}"
        );

        Console.WriteLine(
            $"Falhas: {resultado.Falhas}"
        );

        Console.WriteLine(
            $"Total: {resultado.Total}"
        );

        if (resultado.DetalhesFalhas.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Detalhes das falhas:");
        Console.WriteLine("----------------------------------");

        foreach (var falha in resultado.DetalhesFalhas)
        {
            Console.WriteLine(
                $"Destinatário: {falha.Nome}"
            );

            Console.WriteLine(
                $"E-mail: {falha.Email}"
            );

            Console.WriteLine(
                $"Erro: {falha.Erro}"
            );

            Console.WriteLine("----------------------------------");
        }
    }
}