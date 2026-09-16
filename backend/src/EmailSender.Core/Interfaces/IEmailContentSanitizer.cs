namespace EmailSender.Core.Interfaces;

public interface IEmailContentSanitizer
{
    string Sanitizar (string html);
    string ConverterParaTexto(string html);
    bool PossuiTexto(string htmlSanitizado);
}
