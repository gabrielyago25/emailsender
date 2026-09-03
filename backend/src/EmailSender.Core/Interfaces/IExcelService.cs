using EmailSender.Core.Models;

namespace EmailSender.Core.Interfaces;

public interface IExcelService
{
    ResultadoLeituraPlanilha LerDestinatarios(Stream arquivo);
}