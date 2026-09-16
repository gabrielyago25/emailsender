# Testes automatizados do backend

Execute na raiz do repositório:

```bash
dotnet restore emailsender.slnx
dotnet build emailsender.slnx --no-restore
dotnet test emailsender.slnx --no-build
```

`EmailSender.Infrastructure.Tests` usa xUnit e um cliente SMTP simulado com Moq.
Os testes não acessam servidores SMTP, não usam credenciais reais e não enviam e-mails.

## Resultado SMTP

O retorno bem-sucedido de `SendAsync` confirma a aceitação pelo servidor SMTP,
não a entrega na caixa postal do destinatário. Uma falha ou cancelamento durante
`DisconnectAsync` depois dessa aceitação gera um aviso seguro e mantém o sucesso.
A desconexão graciosa tem orçamento de cinco segundos; o cliente é descartado ao
sair do método, inclusive em caso de falha.

Falhas na conexão, autenticação ou envio continuam sendo propagadas. Não há retry
automático. A classificação de resultados incertos durante o envio e sua
recuperação persistente pertencem às próximas etapas do roadmap.

Os testes cobrem a composição MIME sanitizada, a contagem de sucesso quando a
desconexão falha, a liberação do cliente, o cancelamento e a ausência da mensagem
da exceção nos avisos de desconexão.
