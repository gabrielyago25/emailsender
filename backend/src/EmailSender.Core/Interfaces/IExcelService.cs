using EmailSender.Core.Models;

namespace EmailSender.Core.Interfaces;

public interface IExcelService
{
    List<Destinatario> LerDestinatarios(Stream arquivo);
}