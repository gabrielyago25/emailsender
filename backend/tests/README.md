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

## Validação da mensagem

`EmailSender.Api.Tests` exercita o controller diretamente, com leitura de planilha
simulada e sem iniciar o worker SMTP. Verifica rejeição antes da leitura XLSX e
ausência de trabalho na fila para mensagens inválidas, limites de assunto e corpo
em UTF-8, limites configurados e enfileiramento somente do HTML sanitizado.

São testes do controller, não testes HTTP do pipeline de model binding. Os testes
de infraestrutura também cobrem detecção de texto, listas vazias e caracteres
invisíveis. Consulte os valores e regras em
[Limites da mensagem](../src/EmailSender.Api/Configuration/README.md).

## Falhas e finalização dos jobs

Os testes do `EnvioBackgroundService` executam o worker com dependências simuladas
e verificam mensagens públicas seguras, diagnóstico sem a mensagem da exceção,
continuidade após um job falhar e limpeza do progresso temporário ao finalizar ou
cancelar. Não há conexões SMTP reais.

Falhas por destinatário registram `JobId`, ordem do destinatário e tipo da exceção.
Falhas inesperadas do job registram `JobId` e tipo da exceção. Esses eventos não
incluem o objeto da exceção, sua mensagem, corpo do e-mail ou endereço do
destinatário. Isso limita o detalhe diagnóstico em favor da proteção dos dados;
códigos específicos de provedores podem ser acrescentados futuramente mediante
classificação segura.

`Concluido` continua significando que todos os destinatários foram processados;
o resultado pode conter falhas. Cancelamento não desfaz mensagens já aceitas pelo
SMTP. Persistência e classificação durável de tentativas incertas permanecem
pendentes no roadmap.
