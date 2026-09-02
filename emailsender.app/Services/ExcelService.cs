using ClosedXML.Excel;
using emailsender.app.Models;

namespace emailsender.app.Services;

public class ExcelService
{
    public List<Destinatario> LerDestinatarios(string caminhoArquivo)
    {
        var destinatarios = new List<Destinatario>();

        using var workbook = new XLWorkbook(caminhoArquivo);

        var worksheet = workbook.Worksheet(1);

        var linhas = worksheet.RowsUsed().Skip(1);

        foreach (var linha in linhas)
        {
            var nome = linha.Cell(1).GetString().Trim();
            var email = linha.Cell(2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(email))
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