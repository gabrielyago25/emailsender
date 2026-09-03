namespace EmailSender.Core.Models;

public class ResultadoLeituraPlanilha
{
    public List<Destinatario> DestinatariosValidos { get; set; } = [];

    public List<DestinatarioInvalido> DestinatariosInvalidos { get; set; } = [];

    public int TotalEncontrados => DestinatariosValidos.Count + DestinatariosInvalidos.Count;

    public int TotalValidos => DestinatariosValidos.Count;

    public int TotalInvalidos => DestinatariosInvalidos.Count;
}