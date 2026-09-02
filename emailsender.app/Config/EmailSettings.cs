namespace emailsender.app.Config;

public class EmailSettings{
    public string Host {get; set;} = string.Empty;
    public int Port {get; set;}

    public string Usuario {get; set;} = string.Empty;
    public string Senha {get; set;} = string.Empty;

    public string Remetente {get; set;} = string.Empty;
    public string NomeRemetente {get; set;} = string.Empty;
}