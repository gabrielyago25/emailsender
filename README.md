# EmailSender

Aplicação desenvolvida em C# para automatizar o envio de e-mails a partir de uma lista de destinatários importada de uma planilha Excel (`.xlsx`).

O projeto começou como uma aplicação Console para validar o fluxo de leitura da planilha, configuração SMTP e envio dos e-mails. A proposta é evoluí-lo posteriormente para uma aplicação desktop com interface gráfica.

## Funcionalidades atuais

- Envio de e-mails utilizando SMTP;
- Integração com Gmail através do MailKit;
- Configuração do remetente através de `appsettings.json`;
- Armazenamento seguro da senha utilizando .NET User Secrets;
- Importação de destinatários através de arquivo `.xlsx`;
- Leitura de planilhas utilizando ClosedXML;
- Envio individual para cada destinatário da planilha;
- Definição de assunto e corpo da mensagem;
- Confirmação antes do início dos envios;
- Exibição da quantidade de envios realizados com sucesso e falhas.

## Tecnologias utilizadas

- C#
- .NET 10
- MailKit
- MimeKit
- ClosedXML
- Microsoft.Extensions.Configuration
- .NET User Secrets

## Estrutura atual

```text
emailsender/
│
├── emailsender.sln
│
├── .gitignore
│
└── emailsender.app/
    │
    ├── Config/
    │   └── EmailSettings.cs
    │
    ├── Models/
    │   ├── Destinatario.cs
    │   └── EmailMessage.cs
    │
    ├── Services/
    │   ├── EmailService.cs
    │   └── ExcelService.cs
    │
    ├── appsettings.example.json
    ├── Program.cs
    └── emailsender.app.csproj
```

## Formato da planilha

Atualmente a aplicação espera uma planilha `.xlsx` contendo as seguintes colunas:

| Nome | Email |
|---|---|
| João Silva | joao@exemplo.com |
| Maria Souza | maria@exemplo.com |

A primeira linha é utilizada como cabeçalho e não é considerada como destinatário.

## Configuração

Crie um arquivo `appsettings.json` dentro do projeto `emailsender.app` utilizando como referência o arquivo:

```text
appsettings.example.json
```

Exemplo:

```json
{
  "EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Usuario": "seu-email@gmail.com",
    "NomeRemetente": "Seu Nome",
    "Remetente": "seu-email@gmail.com"
  }
}
```

O arquivo `appsettings.json` não é versionado pelo Git para evitar a publicação de informações pessoais de configuração.

### Senha

A senha utilizada para autenticação SMTP não deve ser adicionada ao código ou ao `appsettings.json`.

O projeto utiliza .NET User Secrets:

```bash
dotnet user-secrets set "EmailSettings:Senha" "SUA-SENHA-DE-APP" --project emailsender.app
```

Para contas Gmail, deve ser utilizada uma senha de aplicativo compatível com a autenticação SMTP.

## Executando o projeto

Restaure as dependências:

```bash
dotnet restore
```

Compile:

```bash
dotnet build
```

Execute:

```bash
dotnet run --project emailsender.app
```

A aplicação solicitará:

```text
Planilha XLSX
     ↓
Lista de destinatários
     ↓
Assunto
     ↓
Corpo do e-mail
     ↓
Confirmação
     ↓
Envio
     ↓
Resultado
```

## Fluxo atual

O arquivo XLSX é processado pelo `ExcelService`, que transforma cada linha da planilha em um `Destinatario`.

Para cada destinatário é criado um `EmailMessage`, que posteriormente é enviado pelo `EmailService` através do MailKit.

```text
Arquivo XLSX
     ↓
ExcelService
     ↓
List<Destinatario>
     ↓
EmailMessage
     ↓
EmailService
     ↓
SMTP
     ↓
Destinatário
```

## Planejamento de evolução

O objetivo é evoluir o projeto para uma aplicação desktop simples para utilização no envio de comunicados.

### Próximas etapas

- [x] Criar aplicação Console;
- [x] Configurar envio SMTP;
- [x] Implementar MailKit;
- [x] Proteger credenciais com User Secrets;
- [x] Implementar leitura de arquivos XLSX;
- [x] Realizar envio para múltiplos destinatários;
- [x] Adicionar confirmação antes do envio;
- [x] Exibir resultado dos envios;
- [ ] Refatorar a lógica de envio para um `EnvioService`;
- [ ] Melhorar validação dos dados da planilha;
- [ ] Disponibilizar um modelo XLSX de destinatários;
- [ ] Criar interface gráfica;
- [ ] Permitir selecionar a planilha pela interface;
- [ ] Criar campos para assunto e corpo do e-mail;
- [ ] Exibir quantidade de destinatários encontrados;
- [ ] Permitir visualizar os destinatários antes do envio;
- [ ] Exibir progresso dos envios;
- [ ] Exibir relatório final de sucessos e falhas;
- [ ] Implementar personalização de mensagens por destinatário.

## Interface planejada

A interface deverá inicialmente disponibilizar:

```text
Assunto
[________________________________]

Corpo do e-mail
[                                ]
[                                ]
[                                ]

Planilha de destinatários
[ Selecionar arquivo XLSX ]

[ Baixar modelo da planilha ]

Destinatários encontrados: 0

                    [ Revisar envio ]
```

Os dados do remetente permanecerão configurados internamente, sem necessidade de preenchimento a cada utilização.

## Segurança

Informações sensíveis não devem ser versionadas no repositório.

Arquivos e informações locais como:

```text
appsettings.json
.env
secrets.json
planilhas de destinatários
```

devem permanecer fora do controle de versão.

A senha SMTP é armazenada utilizando .NET User Secrets durante o desenvolvimento.

## Status

Projeto em desenvolvimento.

A versão atual já permite importar uma lista de destinatários de uma planilha XLSX e realizar o envio dos e-mails através da aplicação Console.