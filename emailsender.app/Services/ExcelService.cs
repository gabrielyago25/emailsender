using ClosedXML.Excel;
using emailsender.app.Models;
using System.Net.Mail;

namespace emailsender.app.Services;

public class ExcelService
{

    private bool EmailValido(string email)
    {
        try
        {
            var endereco = new MailAddress(email);
            return string.Equals(endereco.Address, email, StringComparison.OrdinalIgnoreCase);
        } catch
        {
            return false;
        }
    }
    public List<Destinatario> LerDestinatarios(string caminhoArquivo)
    {
        var destinatarios = new List<Destinatario>();
        var emailsAdicionados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var workbook = new XLWorkbook(caminhoArquivo);
        var worksheet = workbook.Worksheet(1);
        var linhas = worksheet.RowsUsed().Skip(1);


        foreach (var linha in linhas)
        {
            var nome = linha.Cell(1).GetString().Trim();
            var email = linha.Cell(2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(email) || !EmailValido(email) || !emailsAdicionados.Add(email))
            {
                continue;
            }

            destinatarios.Add(new Destinatario
            {
                Nome = nome,
                Email = email
            });
        }

        return destinatarios;
    }
}