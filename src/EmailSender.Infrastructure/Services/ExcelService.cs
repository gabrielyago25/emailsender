using ClosedXML.Excel;
using EmailSender.Core.Models;
using EmailSender.Core.Interfaces;
using System.Net.Mail;

namespace EmailSender.Infrastructure.Services;

public class ExcelService : IExcelService
{
    public List<Destinatario> LerDestinatarios(Stream arquivo)
    {
        var destinatarios = new List<Destinatario>();
        var emailsAdicionados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var workbook = new XLWorkbook(arquivo);
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
}