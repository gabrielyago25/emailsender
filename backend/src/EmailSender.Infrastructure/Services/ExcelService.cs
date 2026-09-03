using ClosedXML.Excel;
using EmailSender.Core.Models;
using EmailSender.Core.Interfaces;
using System.Net.Mail;

namespace EmailSender.Infrastructure.Services;

public class ExcelService : IExcelService
{
    public ResultadoLeituraPlanilha LerDestinatarios(Stream arquivo)
    {
        var resultado = new ResultadoLeituraPlanilha();

        var emailsAdicionados = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );

        using var workbook = new XLWorkbook(arquivo);

        var worksheet = workbook.Worksheet(1);

        var linhas = worksheet
            .RowsUsed()
            .Skip(1);

        foreach (var linha in linhas)
        {
            var numeroLinha = linha.RowNumber();

            var nome = linha
                .Cell(1)
                .GetString()
                .Trim();

            var email = linha
                .Cell(2)
                .GetString()
                .Trim();

            // Ignora somente linhas completamente vazias.
            if (string.IsNullOrWhiteSpace(nome) &&
                string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                resultado.DestinatariosInvalidos.Add(
                    new DestinatarioInvalido
                    {
                        Linha = numeroLinha,
                        Nome = nome,
                        Email = email,
                        Motivo = "E-mail não informado."
                    }
                );

                continue;
            }

            if (!EmailValido(email))
            {
                resultado.DestinatariosInvalidos.Add(
                    new DestinatarioInvalido
                    {
                        Linha = numeroLinha,
                        Nome = nome,
                        Email = email,
                        Motivo = "E-mail inválido."
                    }
                );

                continue;
            }

            // Add retorna:
            // true  -> primeira ocorrência
            // false -> já existia no HashSet
            if (!emailsAdicionados.Add(email))
            {
                resultado.DestinatariosInvalidos.Add(
                    new DestinatarioInvalido
                    {
                        Linha = numeroLinha,
                        Nome = nome,
                        Email = email,
                        Motivo = "E-mail duplicado."
                    }
                );

                continue;
            }

            resultado.DestinatariosValidos.Add(
                new Destinatario
                {
                    Nome = nome,
                    Email = email
                }
            );
        }

        return resultado;
    }

    private static bool EmailValido(string email)
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