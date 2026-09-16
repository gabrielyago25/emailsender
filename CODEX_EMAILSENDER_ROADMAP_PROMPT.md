# Prompt Mestre para Codex — EmailSender

Atue como um **Arquiteto de Software Sênior, Tech Lead e Engenheiro .NET/React** responsável por revisar e planejar a evolução do projeto **EmailSender**.

Você tem acesso ao repositório do projeto.

> **Segurança, integridade dos envios, proteção de credenciais, resiliência e desempenho têm prioridade sobre velocidade de implementação. O objetivo não é apenas produzir código funcional, mas uma aplicação interna que possa ser operada com segurança por múltiplos usuários sem que uma falha, abuso ou carga excessiva comprometa todo o serviço.**

---

## 1. Regras de atuação

1. Primeiro inspecione cuidadosamente o repositório atual.
2. Não assuma que a descrição abaixo corresponde 100% ao código: valide cada ponto no projeto.
3. **NÃO altere nenhum arquivo neste momento.**
4. **NÃO escreva implementação completa ainda.**
5. **NÃO faça migrations, commits, pushes ou comandos destrutivos.**
6. Antes de qualquer implementação, produza uma análise arquitetural e um roadmap.
7. Preserve funcionalidades existentes que já funcionam.
8. Evite overengineering.
9. Prefira evolução incremental, testável e compreensível.
10. Quando encontrar inconsistências entre este documento e o código real, registre-as explicitamente.
11. Se detectar uma vulnerabilidade séria ou uma decisão que possa causar invasão, vazamento, perda de jobs, envio indevido, lentidão significativa ou exaustão de recursos, **pare a implementação, explique o risco, proponha alternativas e aguarde aprovação humana**.

O objetivo desta etapa é **ALINHAR ARQUITETURA E ROADMAP antes de continuar o desenvolvimento**.

---

## 2. Contexto do projeto

O projeto se chama **EmailSender**.

Ele começou como uma aplicação Console em C# e evoluiu para uma aplicação Web interna para envio de e-mails em lote a partir de uma planilha XLSX.

A aplicação será futuramente publicada em um servidor interno da empresa e acessada por vários usuários através de um endereço da rede local.

Estrutura aproximada atual:

```text
EmailSender/
├── backend/
│   └── src/
│       ├── EmailSender.Api/
│       ├── EmailSender.Core/
│       └── EmailSender.Infrastructure/
│
├── frontend/
│   └── React + TypeScript + Vite
│
├── testes/
├── emailsender.slnx
├── README.md
└── .gitignore
```

### Backend

- .NET 10
- ASP.NET Core Web API
- C#
- MailKit / MimeKit
- ClosedXML
- HtmlSanitizer
- AngleSharp

### Frontend

- React
- TypeScript
- Vite
- Tiptap
- DOMPurify

### Persistência

- Ainda não existe banco de dados.
- Jobs atualmente são mantidos em memória.

### SMTP

- Atualmente existe configuração no backend.
- Credenciais não devem entrar no Git.
- Ambiente de desenvolvimento utiliza User Secrets.

---

## 3. Funcionalidades já existentes

Verifique no código se o seguinte fluxo está implementado e funcionando:

```text
Novo envio
    ↓
Assunto
    ↓
Editor Rich Text
    ↓
Importação XLSX
    ↓
Validação dos destinatários
    ↓
Revisão
    ↓
Confirmação
    ↓
Criação de Job
    ↓
Processamento em background
    ↓
SMTP
    ↓
Acompanhamento de progresso
```

A planilha utiliza aproximadamente:

```text
Nome | Email
```

A aplicação deve identificar:

- quantidade de registros encontrados;
- destinatários válidos;
- destinatários inválidos;
- motivo da invalidação;
- duplicidades.

### Regra de duplicidade

- comparação case-insensitive;
- primeira ocorrência é válida;
- ocorrências posteriores são inválidas;
- registros inválidos não são enviados.

Na interface, a intenção é mostrar:

```text
X encontrados
Y válidos
Z inválidos
```

e listar somente os inválidos.

Também existe ou está sendo implementada a funcionalidade:

```text
Baixar modelo da planilha XLSX
```

Verifique o estado real dessa funcionalidade no código.

---

## 4. Rich Text e segurança de HTML

O corpo da mensagem utiliza Tiptap.

Atualmente desejamos suportar:

- negrito;
- itálico;
- sublinhado;
- tamanho da fonte;
- listas;
- desfazer/refazer.

O editor gera HTML.

Na tela de revisão, o HTML deve ser sanitizado antes da renderização.

No backend ele deve ser sanitizado novamente, pois o frontend não é uma barreira de segurança.

Existe uma whitelist restrita de elementos HTML/CSS.

Exemplos permitidos:

```text
p
br
strong
b
em
i
u
span
ul
ol
li
```

CSS necessário:

```text
font-size
```

Conteúdo perigoso, como:

```text
script
onclick
atributos/eventos não permitidos
CSS não autorizado
```

deve ser removido.

O e-mail deve possuir:

```text
text/plain
+
text/html
```

via MimeKit BodyBuilder.

Verifique se isso está implementado corretamente.

---

## 5. Processamento de jobs atual

Procure e analise classes como:

```text
EnvioJob
EnvioJobStore
EnvioJobQueue
EnvioBackgroundService
EnvioService
```

O frontend cria um envio aproximadamente através de:

```http
POST /api/envios
```

e consulta:

```http
GET /api/envios/{id}
```

O acompanhamento atualmente pode utilizar polling.

Analise como a fila atual funciona.

Um ponto importante:

Hoje provavelmente existe **UMA fila/worker central**.

Isso significa que múltiplos usuários podem criar jobs, porém jobs podem acabar sendo serializados globalmente.

Esse comportamento deverá mudar.

---

## 6. Novo objetivo: aplicação multiusuário

A aplicação deverá ser disponibilizada para vários usuários internos simultaneamente.

Exemplo:

```text
PC A ─┐
PC B ─┼── EmailSender interno
PC C ─┘
          ↓
        API
          ↓
      Banco + Workers
```

Um usuário não deve bloquear desnecessariamente outro usuário.

Entretanto precisamos respeitar limites de cada conta SMTP.

---

## 7. Novo conceito: PerfilRemetente

Queremos introduzir o conceito:

```text
PerfilRemetente
```

Exemplos:

```text
Atendimento
Ouvidoria
Financeiro
Administrativo
```

Um perfil poderá possuir aproximadamente:

```text
Id
Nome
Host SMTP
Porta
Usuário SMTP
Credencial SMTP
EmailRemetente
NomeRemetente
Ativo
CriadoEm
AtualizadoEm
```

Avalie e refine esse modelo.

Na tela **Novo envio** deverá existir:

```text
Remetente
[ Atendimento ▼ ]
```

Cada Envio/Job deverá registrar qual `PerfilRemetente` foi utilizado.

---

## 8. Tela de configuração de remetentes

Precisamos criar futuramente uma interface administrativa.

Exemplo conceitual:

```text
Configurações > Remetentes

Perfis cadastrados:

┌──────────────────────────────────────┐
│ Atendimento                          │
│ atendimento@empresa.com              │
│ Ativo                                │
│                        [ Editar ]     │
└──────────────────────────────────────┘
```

Cadastro/Edição:

```text
Nome do perfil
[ Atendimento                         ]

Servidor SMTP
[ smtp.gmail.com                      ]

Porta
[ 587 ]

Usuário SMTP
[ email@empresa.com                   ]

Email remetente
[ email@empresa.com                   ]

Nome exibido
[ Atendimento Empresa                 ]

Senha de app
[ ••••••••••••••                     ]

[ Como obter uma senha de app? ]

Ativo
[✓]

[ Testar conexão ]
[ Salvar ]
```

### Regras obrigatórias

A senha já cadastrada **NUNCA** deve ser retornada pelo backend ao frontend.

Ao editar um perfil existente, preferir algo como:

```text
Senha de app
[ Configurada ]

[ Alterar senha ]
```

O valor anterior não deve ser revelado.

---

## 9. Tutorial expansível da senha de app do Google

Ao lado ou imediatamente abaixo do campo **Senha de app**, deve existir um elemento clicável:

```text
[ ? Como obter uma senha de app do Google? ]
```

ou:

```text
[ Tutorial: gerar senha de app ]
```

Esse elemento **NÃO** deve abrir uma nova tela da aplicação.

Ele deverá expandir/recolher um pequeno painel explicativo, por exemplo utilizando disclosure/accordion.

Exemplo conceitual:

```text
Senha de app
[ •••••••••••••• ]

▶ Como obter uma senha de app do Google?
```

Ao clicar:

```text
▼ Como obter uma senha de app do Google?

1. Acesse sua Conta do Google.
2. Ative a Verificação em duas etapas, caso ainda não esteja ativa.
3. Acesse a área "Senhas de app" da Conta do Google.
4. Faça login novamente se solicitado.
5. Crie uma nova senha de app para o EmailSender.
6. O Google fornecerá uma senha de app de 16 caracteres/dígitos.
7. Copie essa senha e cole no campo "Senha de app" do EmailSender.
8. Salve o perfil.
```

Exibir também um aviso:

> **Não utilize a senha normal da sua Conta do Google neste campo.**

E:

> **A senha de app é exibida pelo Google apenas no momento da criação. Caso ela seja perdida, gere uma nova.**

Também deve existir um link:

```text
[Abrir página oficial do Google]
```

Esse link deverá abrir a documentação/página oficial do Google em uma nova aba utilizando:

```html
target="_blank"
rel="noopener noreferrer"
```

### Segurança específica do tutorial/campo

- Não armazenar a senha de app no frontend.
- Não colocar a senha em localStorage/sessionStorage.
- Não registrar a senha em logs.
- Não retornar a senha nas respostas da API.
- Não incluir credenciais em mensagens de erro.
- O tutorial é apenas explicativo.

Considere ainda que a opção **Senhas de app** pode não estar disponível em determinados cenários, como contas organizacionais com políticas específicas, contas com Proteção Avançada ou algumas configurações de autenticação.

A interface deverá informar de forma curta:

> Se a opção Senhas de app não estiver disponível, a configuração da sua Conta Google ou da organização pode não permitir esse recurso. Consulte o administrador.

### Regra de arquitetura

Apesar desse tutorial inicialmente focar no Google/Gmail, **não acople o domínio `PerfilRemetente` exclusivamente ao Gmail**.

O `PerfilRemetente` deve continuar genérico para permitir outros provedores SMTP futuramente.

Avalie se o tutorial deve aparecer:

A. sempre;  
B. apenas quando Host/Provedor for Gmail;  
C. através de uma ajuda específica por provedor.

Recomende a solução mais sustentável.

---

## 10. Teste de conexão SMTP

Avalie adicionar ao cadastro do remetente:

```text
[ Testar conexão ]
```

Esse recurso deveria validar configuração antes de salvar ou ativar o perfil.

Avalie:

- endpoint apropriado;
- timeout;
- tratamento de erro;
- segurança;
- se devemos apenas conectar/autenticar ou também enviar mensagem de teste;
- proteção contra abuso;
- logging sem credenciais.

**Não implemente ainda.**

Proponha primeiro o comportamento recomendado.

---

## 11. Concorrência por remetente

Esta é uma regra arquitetural essencial.

Queremos:

```text
PARALELISMO ENTRE REMETENTES DIFERENTES
```

mas:

```text
SERIALIZAÇÃO ENTRE JOBS DO MESMO REMETENTE
```

Exemplo:

```text
Usuário A:
Perfil = Atendimento
Job A

Usuário B:
Perfil = Ouvidoria
Job B
```

Os dois podem executar simultaneamente.

Por outro lado:

```text
Usuário A:
Perfil = Atendimento
Job A

Usuário B:
Perfil = Atendimento
Job C
```

Job C deve aguardar Job A.

Representação:

```text
Atendimento:
Job A → Job C → Job F

Ouvidoria:
Job B → Job D

Financeiro:
Job E
```

Cada remetente pode ter seu próprio fluxo serial:

```text
Atendimento ─────────►
Ouvidoria   ─────────►
Financeiro  ─────────►
```

Mas os fluxos podem ocorrer em paralelo entre si.

---

## 12. Rate limiting

Hoje existe aproximadamente um intervalo entre mensagens.

Queremos que o rate limiting passe a pertencer ao `PerfilRemetente` ou à política associada ao remetente.

Exemplo:

```text
Atendimento:
1 mensagem / 60 segundos

Ouvidoria:
1 mensagem / 60 segundos
```

Os timers não devem interferir entre contas diferentes.

Analise alternativas como:

- Channel por remetente;
- SemaphoreSlim por remetente;
- scheduler central;
- workers dinâmicos;
- background queue persistente;
- outra solução adequada para ASP.NET Core.

Não escolha apenas pela simplicidade do exemplo.

Compare vantagens e desvantagens considerando:

- concorrência;
- reinício do servidor;
- escalabilidade;
- consistência;
- complexidade;
- manutenção;
- futura execução em múltiplas instâncias da API.

Faça uma recomendação.

---

## 13. PostgreSQL + Entity Framework Core

A aplicação atualmente não possui persistência permanente.

Queremos avaliar introduzir:

```text
PostgreSQL
Entity Framework Core
```

Possíveis entidades:

```text
PerfilRemetente
Envio
DestinatarioEnvio ou ResultadoDestinatario
FalhaEnvio
```

E posteriormente:

```text
Usuario
Permissao
```

Analise o modelo.

Não crie migrations ainda.

Proponha:

- entidades;
- relacionamentos;
- índices;
- constraints;
- campos de auditoria;
- enums/status;
- estratégia de migrations.

---

## 14. Jobs persistentes

Jobs atualmente podem estar apenas em memória.

Isso não é suficiente para ambiente compartilhado.

Precisamos definir comportamento para:

- API reiniciada durante envio;
- servidor reiniciado;
- processo interrompido;
- job pendente;
- job em andamento abandonado;
- destinatário já enviado antes de uma falha;
- retries;
- duplicação acidental.

Considere conceitos como:

```text
Pending
Running
Waiting
Completed
Failed
Cancelled
Interrupted
```

Analise se precisamos persistir progresso por destinatário para evitar reenviar mensagens que já foram enviadas antes de uma interrupção.

---

## 15. Idempotência

Avalie explicitamente risco de envio duplicado.

Exemplo:

```text
Frontend chama POST /api/envios.
Servidor aceita.
Resposta ao navegador falha.
Usuário clica novamente.
```

Não queremos dois envios idênticos acidentalmente.

Proponha estratégia de idempotência apropriada para criação de jobs.

Considere:

```text
Idempotency-Key
```

ou mecanismo equivalente.

Explique:

- geração;
- armazenamento;
- expiração;
- comportamento em retry;
- conflitos.

---

## 16. Segurança das credenciais SMTP

Credenciais SMTP **NÃO** podem:

- estar em texto puro sem proteção;
- entrar no Git;
- aparecer em logs;
- aparecer em responses;
- ser devolvidas em GET;
- aparecer no HTML;
- aparecer no histórico.

Avalie opções adequadas ao nosso cenário interno:

- ASP.NET Core Data Protection;
- criptografia em nível de aplicação;
- secrets do ambiente;
- secret store;
- chave de criptografia fornecida pelo servidor;
- soluções externas de secret management.

Evite complexidade empresarial desnecessária.

Entretanto, a solução precisa ser razoavelmente segura para produção interna.

Explique também:

- onde ficará a chave usada para proteger os secrets;
- comportamento após restart;
- comportamento com Docker;
- backup/restauração;
- rotação de credenciais.

Nenhuma chave usada para proteger secrets pode ficar armazenada no mesmo banco como simples texto juntamente com os secrets protegidos.

---

## 17. Autenticação e autorização

A aplicação futuramente deverá possuir autenticação.

Inicialmente pensamos em dois papéis:

### USUÁRIO

Pode:

- criar envio;
- escolher remetentes permitidos;
- acompanhar envio;
- consultar histórico permitido.

### ADMINISTRADOR

Pode:

- tudo acima;
- cadastrar remetente;
- editar remetente;
- alterar credencial;
- desativar remetente;
- acessar configurações administrativas.

Analise quando autenticação deve ser implementada no roadmap.

Considere que a aplicação estará em rede interna, mas **NÃO trate rede interna como mecanismo de autenticação suficiente**.

Avalie também se usuários deveriam ter permissão apenas para determinados `PerfisRemetente`.

---

## 18. Histórico

Queremos futuramente uma tela:

```text
Histórico de envios
```

Campos:

```text
Data/Hora
Remetente
Assunto
Usuário responsável
Total
Enviados
Falhas
Status
```

Exemplo:

```text
08/09/2026  Atendimento  Comunicado X  Gabriel  25/25  Concluído
07/09/2026  Ouvidoria    Pendências    João     18/20  2 falhas
```

Ao abrir um item:

- informações do envio;
- remetente utilizado;
- status;
- horários;
- total;
- enviados;
- falhas;
- erros por destinatário quando aplicável.

Avalie questões de privacidade e retenção.

Evite armazenar desnecessariamente o corpo completo do e-mail se não houver justificativa.

---

## 19. Frontend

O frontend já possui ou deverá possuir:

- Novo envio;
- Revisão;
- Acompanhamento;
- Editor Rich Text;
- Upload XLSX;
- Validação XLSX;
- Download do modelo.

Precisaremos adicionar:

```text
Configurações
└── Remetentes
```

Possivelmente:

```text
Histórico
```

E posteriormente:

```text
Administração de usuários/permissões
```

Analise se já faz sentido introduzir:

```text
React Router
```

Sugira uma organização de frontend sustentável.

Exemplo possível:

```text
src/
├── components/
├── pages/
├── services/
├── types/
├── hooks/
├── layouts/
└── routes/
```

Mas valide contra o projeto real.

---

## 20. Experiência do usuário

A interface atual busca ser:

- simples;
- limpa;
- profissional;
- sem excesso de elementos;
- adequada para aplicação interna.

Preserve essa característica.

Evite transformar a aplicação em um painel administrativo excessivamente complexo.

O fluxo principal deve continuar rápido:

```text
Novo envio
→ escolher remetente
→ assunto
→ corpo
→ planilha
→ revisão
→ confirmar
→ acompanhar
```

---

## 21. Docker e deploy

A aplicação será futuramente publicada em servidor interno.

Planejamos:

```text
Backend container
Frontend container
PostgreSQL container
```

Possivelmente:

```text
reverse proxy
```

Objetivo operacional:

```bash
docker compose up -d --build
```

Credenciais e chaves **NÃO** deverão ser embutidas nas imagens.

Analise:

- Dockerfiles;
- docker-compose;
- volumes;
- PostgreSQL;
- variáveis de ambiente;
- secrets;
- HTTPS/reverse proxy;
- health checks;
- backups;
- logs.

Não implemente ainda.

---

## 22. Observabilidade

Como haverá envios em background, proponha estratégia simples para:

- logging estruturado;
- JobId;
- PerfilRemetenteId;
- início/fim de job;
- falhas SMTP;
- falhas por destinatário;
- tempo de processamento.

Nunca registrar:

- senha SMTP;
- senha de app;
- conteúdo sensível desnecessário.

Avalie se Serilog ou logging nativo do ASP.NET Core é suficiente neste estágio.

---

## 23. Estratégia de testes

Defina estratégia de testes.

### ExcelService

- email válido;
- inválido;
- vazio;
- duplicado;
- duplicado com casing diferente;
- linha vazia.

### EmailContentSanitizer

- HTML permitido;
- script removido;
- onclick removido;
- CSS não permitido removido;
- font-size permitido;
- geração text/plain.

### EnvioService

- sucesso;
- falha;
- cancelamento;
- progresso;
- rate limiting.

### Concorrência

- jobs do mesmo remetente não executam simultaneamente;
- jobs de remetentes diferentes executam simultaneamente.

### Persistência

- retomada após restart;
- não duplicação de destinatário.

### API

- validações;
- autorização;
- idempotência.

### Frontend

- fluxos principais onde realmente agregue valor.

---

# 24. Requisitos não negociáveis: segurança, resiliência e desempenho

Segurança, estabilidade e desempenho são requisitos de primeira classe deste projeto.

Não trate segurança como uma etapa final de revisão.

Toda decisão arquitetural, endpoint, entidade, worker, fila, formulário, integração SMTP, configuração, migration, container e fluxo de autenticação deve ser avaliado também por:

- superfície de ataque;
- autorização;
- exposição de dados;
- concorrência;
- consumo de CPU;
- consumo de memória;
- consumo de conexões;
- crescimento de filas;
- crescimento de banco;
- latência;
- indisponibilidade de dependências;
- comportamento sob falha;
- comportamento sob abuso;
- comportamento sob carga simultânea.

A aplicação será disponibilizada em ambiente interno, mas:

> **REDE INTERNA NÃO DEVE SER CONSIDERADA UMA FRONTEIRA DE SEGURANÇA SUFICIENTE.**

Assuma que:

- usuários podem cometer erros;
- usuários autenticados podem tentar ações para as quais não possuem permissão;
- uma máquina interna pode estar comprometida;
- requisições podem ser manipuladas fora do frontend;
- endpoints podem ser chamados diretamente;
- uploads podem ser malformados ou maliciosos;
- serviços externos podem ficar lentos;
- SMTP pode travar ou responder lentamente;
- banco pode ficar indisponível;
- containers podem reiniciar;
- a aplicação pode receber várias requisições simultaneamente.

A arquitetura deve ser segura por padrão e falhar de maneira controlada.

---

## 25. Threat model obrigatório

Antes de implementar funcionalidades multiusuário, produza um threat model simplificado do sistema.

Identifique pelo menos:

- ativos que precisam ser protegidos;
- credenciais SMTP;
- contas de usuários;
- sessões/tokens;
- dados dos destinatários;
- histórico de envios;
- configurações administrativas;
- banco de dados;
- arquivos XLSX;
- conteúdo dos e-mails;
- logs;
- chaves de criptografia.

Mapeie os trust boundaries entre:

```text
Browser
→ Reverse Proxy
→ API
→ Workers
→ PostgreSQL
→ SMTP
→ armazenamento de secrets
```

Analise ameaças relacionadas a:

- autenticação;
- autorização;
- escalada de privilégio;
- XSS;
- CSRF quando aplicável;
- SQL Injection;
- HTML Injection;
- upload malicioso;
- path traversal;
- SSRF;
- brute force;
- credential stuffing;
- enumeração de recursos;
- IDOR/BOLA;
- replay;
- envio duplicado;
- abuso de SMTP;
- negação de serviço;
- exaustão de filas;
- exaustão de memória;
- exaustão de conexões;
- vazamento de informações;
- logs sensíveis;
- configuração insegura.

Para cada ameaça relevante informe:

- Risco;
- Impacto;
- Probabilidade;
- Mitigação;
- Onde deve ser mitigada;
- Como deverá ser testada.

Não implemente uma solução apenas porque a aplicação está em rede interna.

---

## 26. Autenticação e autorização — requisitos de segurança

Nenhum endpoint administrativo deve depender apenas da interface para proteção.

A autorização deve acontecer obrigatoriamente no backend.

Avalie:

- autenticação adequada ao ambiente empresarial;
- expiração de sessão;
- proteção de cookies/tokens;
- logout;
- política de senha quando aplicável;
- bloqueios/rate limit para tentativas de autenticação;
- roles;
- policies;
- autorização por PerfilRemetente.

Nunca confiar em identificadores ou permissões enviados pelo frontend.

Sempre validar no backend:

```text
Usuário autenticado
+
Permissão
+
Recurso solicitado
```

Analise riscos de IDOR/BOLA para endpoints como:

```http
GET /api/envios/{id}
```

O usuário não deve conseguir visualizar ou manipular um envio apenas porque conhece seu Guid.

---

## 27. Credenciais SMTP e secrets

Credenciais são consideradas informação altamente sensível.

Nunca:

- enviar senha SMTP em respostas;
- registrar senha em logs;
- armazenar senha em localStorage;
- armazenar senha em sessionStorage;
- incluir senha em exceptions;
- incluir senha em telemetry;
- incluir senha em commits;
- incluir senha em imagens Docker;
- retornar a senha mesmo para administradores;
- enviar a senha atual ao frontend durante edição.

A solução deve explicar claramente:

- como a credencial é protegida em repouso;
- onde fica a chave de criptografia;
- como essa chave é disponibilizada ao servidor;
- comportamento em Docker;
- comportamento após restart;
- comportamento em backup/restore;
- rotação da chave;
- rotação da senha SMTP;
- recuperação em desastre.

---

## 28. Configuração SMTP e SSRF

Considere que permitir que um administrador informe:

```text
Host SMTP
Porta
```

pode transformar o servidor em um mecanismo para estabelecer conexões arbitrárias.

Trate isso como possível vetor de SSRF/conexão indevida.

Avalie:

- quais usuários podem alterar host/porta;
- validação de host;
- validação de porta;
- lista de provedores aprovados;
- allowlist opcional;
- DNS resolution;
- comportamento com hosts internos;
- localhost;
- endereços especiais;
- timeouts.

Não permita que usuários comuns forneçam arbitrariamente destinos de rede para o servidor conectar.

O endpoint **Testar conexão SMTP** merece análise específica de abuso.

Deve possuir:

- autorização administrativa;
- timeout curto;
- cancellation;
- rate limiting;
- logging seguro;
- mensagens de erro sanitizadas.

Nunca retornar exceções SMTP completas contendo informações internas ao frontend.

---

## 29. Input validation

Todos os dados externos devem ser tratados como não confiáveis.

Validar no backend:

- strings obrigatórias;
- limites máximos;
- formato;
- enums;
- Guids;
- paginação;
- arquivos;
- MIME quando aplicável;
- extensão;
- tamanho;
- estrutura XLSX;
- número máximo de registros;
- assunto;
- corpo;
- parâmetros SMTP.

Não confiar nas validações React.

Frontend fornece UX.

Backend fornece segurança.

---

## 30. Upload XLSX e prevenção de abuso

A importação de XLSX deve possuir limites.

Avalie:

- tamanho máximo do arquivo;
- quantidade máxima de linhas;
- quantidade máxima de destinatários;
- comportamento com planilhas corrompidas;
- planilhas extremamente complexas;
- arquivos renomeados para `.xlsx`;
- consumo de memória do ClosedXML;
- tempo máximo de processamento.

A aplicação não deve aceitar arquivos arbitrariamente grandes.

Evite carregar múltiplas cópias desnecessárias do arquivo em memória.

Considere proteção contra ataques de exaustão de memória/CPU por upload.

Explique os limites recomendados e torne-os configuráveis quando fizer sentido.

---

## 31. HTML e XSS

O conteúdo Rich Text deve continuar usando defesa em profundidade.

Frontend:

```text
DOMPurify
→ proteção da visualização
```

Backend:

```text
HtmlSanitizer
→ proteção real do conteúdo aceito
```

Nunca considerar DOMPurify no navegador como controle suficiente.

Mantenha uma allowlist mínima de:

- tags;
- atributos;
- propriedades CSS.

Evite liberar HTML/CSS adicional sem justificativa.

Teste explicitamente:

```text
script
onclick
onerror
javascript:
style não permitido
elementos embutidos
HTML malformado
```

---

## 32. Rate limiting da API

Avalie rate limiting também para os endpoints HTTP.

Não confundir:

```text
SMTP Rate Limiting
```

com:

```text
API Rate Limiting
```

A API deve possuir proteção adequada contra abuso de:

- login;
- criação de envio;
- upload de planilha;
- teste SMTP;
- download;
- polling;
- endpoints administrativos.

Não aplicar limites arbitrários.

Defina limites de acordo com risco e uso esperado.

---

## 33. Filas com backpressure

**NÃO use estruturas ilimitadas sem avaliar crescimento.**

Evite:

```csharp
Channel.CreateUnbounded(...)
```

ou equivalentes para uma fila que poderá receber jobs indefinidamente em produção, salvo justificativa arquitetural muito forte.

Analise:

- bounded channels;
- capacidade máxima;
- backpressure;
- persistência no banco;
- política quando fila estiver cheia;
- prioridade;
- limite global;
- limite por remetente.

Uma pessoa não deve conseguir gerar milhares de jobs e consumir memória indefinidamente.

---

## 34. Concorrência controlada

Queremos paralelismo entre remetentes, mas **NÃO concorrência ilimitada**.

Defina:

- limite de workers globais;
- limite por PerfilRemetente;
- limite de conexões SMTP simultâneas;
- limite de jobs ativos;
- limite de jobs pendentes.

Exemplo conceitual:

```text
Atendimento → 1 worker
Ouvidoria   → 1 worker
Financeiro  → 1 worker
```

Porém, se existirem 500 perfis cadastrados, isso **NÃO** deve significar automaticamente 500 workers executando simultaneamente.

Deve existir também um limite global configurável.

Explique como evitar:

- thread starvation;
- connection exhaustion;
- memory pressure;
- DB connection pool exhaustion;
- SMTP connection exhaustion.

---

## 35. Multi-instância

Não assuma para sempre que haverá apenas uma instância da API.

Mesmo que a primeira implantação tenha apenas uma instância, avalie se a solução escolhida quebra quando houver:

```text
API-1
API-2
```

Exemplos de riscos:

```text
SemaphoreSlim em memória
Dictionary em memória
Channel em memória
locks locais
```

não coordenam instâncias diferentes.

Identifique claramente:

> **Funciona apenas em single-instance**

quando for o caso.

Para jobs persistentes, avalie mecanismos distribuídos apropriados, por exemplo:

- reserva transacional no banco;
- row locking;
- optimistic concurrency;
- lease;
- PostgreSQL SKIP LOCKED;
- advisory locks;
- outra estratégia tecnicamente justificada.

Não implemente complexidade distribuída prematuramente, mas não esconda a limitação.

---

## 36. Timeouts

**NENHUMA operação externa deve depender de timeout infinito.**

Defina timeouts explícitos para:

- SMTP connect;
- SMTP authentication;
- SMTP send;
- queries críticas;
- teste SMTP;
- chamadas HTTP futuras;
- operações longas quando aplicável.

Toda operação assíncrona relevante deve considerar:

```text
CancellationToken
```

---

## 37. Retries

Não aplicar retry cegamente.

Classifique erros entre:

### Transitórios

Exemplo:

```text
falha temporária de rede
```

### Permanentes

Exemplo:

```text
senha inválida
destinatário inválido
autenticação SMTP negada
```

Retry somente quando fizer sentido.

Quando aplicável, considere:

- exponential backoff;
- jitter;
- limite máximo de tentativas.

Evite retry storms.

---

## 38. Circuit breaking e dependências lentas

Avalie se mecanismos de circuit breaking são necessários para SMTP ou outros serviços externos.

Um provedor SMTP indisponível não deve:

- travar todos os workers;
- bloquear remetentes independentes;
- consumir threads indefinidamente;
- criar loops infinitos de retry.

Falha no remetente A não deve necessariamente parar remetente B.

---

## 39. Banco de dados

Ao introduzir PostgreSQL:

- utilizar queries parametrizadas/EF Core;
- criar índices necessários;
- evitar N+1;
- evitar carregar grandes coleções em memória;
- usar paginação;
- avaliar `AsNoTracking` para leitura;
- usar transações onde necessário;
- garantir constraints no banco;
- definir limites para strings;
- usar concurrency control quando necessário.

Analise índices especialmente para:

```text
Envio.Status
Envio.PerfilRemetenteId
Envio.CriadoEm
Envio.UsuarioId
```

e tabelas relacionadas.

Não criar índices indiscriminadamente.

---

## 40. Polling e carga da API

O frontend atualmente pode consultar:

```http
GET /api/envios/{id}
```

periodicamente.

Analise o custo de:

```text
1 usuário
10 usuários
100 usuários
múltiplos jobs simultâneos
```

Não permitir polling excessivamente agressivo.

Compare:

- polling;
- long polling;
- SSE;
- SignalR.

Não substituir polling apenas porque existe tecnologia mais sofisticada.

Escolha com base em:

- carga;
- complexidade;
- quantidade esperada de usuários;
- experiência necessária.

---

## 41. Paginação

Endpoints de histórico nunca devem retornar quantidade ilimitada de registros.

Utilizar paginação.

Definir limites máximos de:

- pageSize;
- filtros;
- período consultado quando fizer sentido.

Não permitir:

```text
GET /historico → carregar milhões de registros
```

---

## 42. Observabilidade sem vazamento

Logs devem possibilitar diagnosticar problemas de produção.

Use identificadores como:

```text
JobId
PerfilRemetenteId
UsuarioId
CorrelationId
```

Registre eventos como:

```text
JobCriado
JobIniciado
MensagemEnviada
MensagemFalhou
JobConcluido
JobInterrompido
SMTPIndisponivel
```

Nunca registrar:

- senha;
- senha de app;
- token;
- cookie;
- Authorization header;
- HTML completo do e-mail sem necessidade;
- lista inteira de destinatários sem necessidade.

Avalie mascaramento de informações.

Logs também precisam de:

- rotação;
- retenção;
- limite de tamanho.

para evitar que o disco seja preenchido.

---

## 43. Tratamento de erros

Nunca retornar stack trace ao usuário em produção.

Frontend deve receber mensagens seguras.

Detalhes internos devem ficar em logs apropriados.

Utilizar tratamento global de exceções quando adequado.

Diferenciar:

```text
400
401
403
404
409
422 quando apropriado
429
500
503
```

Evitar responder 500 para erros previsíveis de negócio.

---

## 44. Headers e transporte

Para ambiente de produção, avaliar:

- HTTPS;
- HSTS;
- Secure cookies;
- HttpOnly;
- SameSite;
- CSP;
- X-Content-Type-Options;
- Referrer-Policy;
- `frame-ancestors` / proteção contra clickjacking.

CORS deve permitir apenas origens conhecidas.

Nunca usar em produção sem justificativa:

```csharp
AllowAnyOrigin()
```

especialmente combinado com credenciais.

---

## 45. CSRF

Quando a estratégia de autenticação for definida, analisar explicitamente CSRF.

Se autenticação utilizar cookies, implementar proteção adequada.

Não assumir que SPA elimina CSRF automaticamente.

---

## 46. Dependências

Avalie dependências NuGet e npm.

Antes da produção:

- identificar vulnerabilidades conhecidas;
- evitar pacotes abandonados sem necessidade;
- manter lockfiles;
- revisar atualizações;
- documentar dependências críticas.

Nenhuma vulnerabilidade **CRÍTICA** ou **ALTA** conhecida deve ser levada conscientemente para produção sem:

- mitigação;
- justificativa;
- aprovação explícita.

---

## 47. Containers

Quando Docker for introduzido:

- não executar como root quando desnecessário;
- imagens mínimas;
- multi-stage build;
- nenhuma credencial dentro da imagem;
- health checks;
- limites de recursos quando apropriado;
- filesystem/volumes planejados;
- versão de imagem controlada;
- evitar tags ambiguamente mutáveis quando for relevante.

---

## 48. PostgreSQL em produção

Planejar:

- volume persistente;
- backup;
- restore;
- retenção;
- usuário da aplicação com privilégios mínimos;
- não usar superuser;
- conexão protegida conforme ambiente;
- connection pool adequado;
- health check.

A aplicação não deve possuir privilégios administrativos no PostgreSQL sem necessidade.

---

## 49. Resource limits

Planejar limites para evitar lentidão global.

Avalie limites de:

- tamanho XLSX;
- linhas XLSX;
- destinatários por job;
- jobs por usuário;
- jobs por remetente;
- jobs globais;
- workers;
- conexões SMTP;
- conexões ao banco;
- tamanho de body HTML;
- tamanho de assunto;
- histórico consultado;
- logs;
- retries.

Os valores devem ser configuráveis quando apropriado.

Não invente números arbitrários.

Primeiro proponha valores iniciais com justificativa e estratégia para medir/ajustar.

---

## 50. Graceful shutdown

Quando a aplicação receber sinal de encerramento:

```text
SIGTERM
container stop
restart
```

o sistema deve possuir comportamento definido.

Analise:

- parar de aceitar novos jobs;
- concluir operação atual quando seguro;
- liberar locks/leases;
- marcar jobs interrompidos;
- permitir recuperação após restart.

Não permitir que jobs fiquem eternamente com `Status=Running` após crash.

---

## 51. Recuperação após falhas

Defina comportamento para:

- PostgreSQL indisponível;
- SMTP indisponível;
- disco cheio;
- container reiniciado;
- rede interrompida;
- credencial revogada;
- processo encerrado durante `SendAsync`.

Especial atenção:

Após uma falha durante envio SMTP, pode não ser possível saber com 100% de certeza se o servidor SMTP aceitou a mensagem antes da conexão cair.

Analise explicitamente o problema de **exactly once delivery**.

Não prometa exatamente uma vez quando a infraestrutura SMTP não permite garantir isso.

Projete para minimizar duplicações e registrar estados incertos quando necessário.

---

## 52. Auditoria

Ações administrativas importantes devem ser auditáveis.

Exemplos:

- PerfilRemetente criado;
- PerfilRemetente alterado;
- PerfilRemetente desativado;
- Credencial alterada;
- Permissão alterada.

Registrar:

```text
quem
quando
qual recurso
qual ação
```

Nunca registrar a senha anterior/nova.

---

## 53. Proteção contra erros operacionais

Não proteger apenas contra atacantes.

Também proteger contra erros humanos.

Exemplos:

- confirmar antes de grandes envios;
- informar total de destinatários;
- identificar inválidos;
- impedir duplo clique;
- prevenir submissão duplicada;
- mostrar remetente selecionado claramente;
- alertar sobre configuração desativada;
- validar conexão SMTP;
- evitar exclusões irreversíveis sem confirmação.

Avalie se existe necessidade de limite ou confirmação adicional para envios muito grandes.

---

## 54. Performance e capacidade

Antes da produção, estabelecer uma expectativa de capacidade.

Não precisa criar uma arquitetura para milhões de usuários.

Precisamos saber aproximadamente:

- usuários internos simultâneos;
- quantidade típica de destinatários;
- quantidade máxima de destinatários;
- jobs por dia;
- jobs simultâneos;
- perfis SMTP ativos.

Com base nisso, proponha:

- limites iniciais;
- número de workers;
- capacidade de fila;
- intervalos de polling;
- connection pool;
- recursos mínimos do servidor.

Não otimizar prematuramente.

Mas também não criar estruturas sem limite.

---

## 55. Load testing

Antes do deploy de produção, defina testes de carga.

Cenários mínimos:

- vários usuários acessando ao mesmo tempo;
- múltiplos uploads;
- múltiplos jobs;
- vários remetentes em paralelo;
- grande número de registros no histórico;
- SMTP lento;
- SMTP indisponível;
- PostgreSQL sob carga.

A finalidade não é apenas descobrir throughput.

Medir:

- latência;
- CPU;
- memória;
- conexões;
- tamanho da fila;
- tempo de resposta;
- erros;
- tempo de processamento.

---

## 56. Health checks

Avalie health checks separados para:

```text
liveness
readiness
```

A aplicação pode estar viva, mas incapaz de processar trabalho.

Considere estado de:

- API;
- PostgreSQL.

Tenha cuidado para não transformar indisponibilidade temporária do SMTP de um único perfil em "API inteira morta".

---

## 57. Failure isolation

Falha de um `PerfilRemetente` não deve derrubar os demais.

Exemplo:

```text
Atendimento
→ credencial revogada

Ouvidoria
→ funcionando
```

O worker de Atendimento deve falhar de maneira isolada.

Ouvidoria deve continuar.

Da mesma forma:

- um XLSX malformado;
- um Job inválido;
- uma exceção inesperada;

não deve derrubar o `BackgroundService` inteiro.

---

## 58. Backpressure e degradação controlada

Quando o sistema estiver sobrecarregado:

**NÃO continuar aceitando trabalho indefinidamente.**

Prefira degradação controlada.

Por exemplo:

- recusar novos jobs temporariamente;
- responder 429/503 quando apropriado;
- informar fila cheia;
- manter trabalhos existentes íntegros.

O sistema deve permanecer administrável durante sobrecarga.

---

## 59. Revisão de segurança por fase

Para **CADA fase do roadmap**, inclua:

### SECURITY IMPACT

Responda:

- nova superfície de ataque?
- novo dado sensível?
- nova permissão?
- nova conexão externa?
- novo endpoint?
- novo risco de DoS?
- novo risco de vazamento?
- nova dependência?

### PERFORMANCE IMPACT

Responda:

- aumenta uso de memória?
- aumenta queries?
- cria polling?
- cria background work?
- aumenta conexões?
- exige índice?
- exige limite?
- pode crescer indefinidamente?

---

## 60. Security gates

Antes de considerar uma fase pronta:

1. build precisa passar;
2. testes relevantes precisam passar;
3. nenhum secret deve estar no Git;
4. nenhum novo endpoint deve estar sem validação;
5. autorização deve estar aplicada onde necessário;
6. limites precisam estar definidos para entradas potencialmente grandes;
7. logs não podem expor secrets;
8. falhas previsíveis devem ser tratadas;
9. timeouts devem existir para I/O externo;
10. cancellation deve ser considerada;
11. concorrência deve ser limitada;
12. estruturas potencialmente ilimitadas devem ser justificadas;
13. riscos de segurança da fase devem estar documentados.

---

## 61. Production gate

Antes de recomendar deploy em produção, produza uma checklist específica.

**NÃO considerar pronto para produção enquanto existirem riscos críticos/altos conhecidos sem mitigação ou aceitação explícita.**

A checklist deve incluir pelo menos:

- Autenticação;
- Autorização;
- Secrets;
- HTTPS;
- CORS;
- CSRF;
- Headers;
- Rate limiting;
- Input validation;
- XSS;
- Uploads;
- SQL/DB;
- SMTP;
- SSRF;
- Idempotência;
- Filas;
- Concorrência;
- Timeouts;
- Retries;
- Logs;
- Health checks;
- Backup;
- Restore;
- Containers;
- Resource limits;
- Dependencies;
- Load testing;
- Graceful shutdown;
- Recovery testing;
- Auditoria.

---

## 62. Prioridade entre segurança e conveniência

Se existir conflito entre:

```text
conveniência de implementação
```

e:

```text
proteção de credenciais/dados/integridade
```

priorize segurança.

Se existir conflito entre:

```text
paralelismo máximo
```

e:

```text
estabilidade
```

priorize concorrência controlada.

Se existir conflito entre:

```text
aceitar todo trabalho
```

e:

```text
manter o sistema saudável
```

priorize backpressure e degradação controlada.

---

## 63. Regra obrigatória para implementação futura

Sempre que estiver prestes a implementar uma etapa, antes de alterar código apresente:

1. O que será alterado.
2. Por que.
3. Riscos de segurança.
4. Riscos de desempenho.
5. Riscos de concorrência.
6. Estratégia de rollback.
7. Como testar.
8. Critério de conclusão.

Se detectar uma vulnerabilidade séria ou uma decisão arquitetural que possa causar:

- invasão;
- vazamento de credenciais;
- vazamento de dados;
- envio indevido;
- duplicação massiva;
- exaustão de recursos;
- deadlock;
- starvation;
- fila infinita;
- perda de jobs;
- corrupção de dados;
- lentidão significativa;

**PARE.**

Não continue simplesmente seguindo o roadmap.

Explique o problema, proponha alternativas e aguarde aprovação humana.

---

## 64. Sua primeira tarefa

**NÃO IMPLEMENTE NADA AINDA.**

Primeiro:

1. Inspecione todo o repositório relevante.
2. Apresente a arquitetura atual REAL encontrada.
3. Compare-a com o contexto deste prompt.
4. Liste divergências.
5. Identifique dívida técnica.
6. Separe riscos em:
   - imediato;
   - multiusuário;
   - produção.
7. Desenhe a arquitetura-alvo recomendada.
8. Defina o modelo conceitual das entidades.
9. Defina estratégia para `PerfisRemetente`.
10. Defina estratégia de armazenamento seguro de credenciais.
11. Defina estratégia de concorrência por remetente.
12. Defina estratégia de jobs persistentes e recuperação.
13. Defina estratégia de idempotência.
14. Defina autenticação/autorização.
15. Defina como encaixar histórico.
16. Defina estratégia de deploy.
17. Produza threat model.
18. Produza análise de capacidade/performance.
19. Identifique limites e backpressure necessários.
20. Produza cenários de falha e recuperação.

---

## 65. Roadmap obrigatório

Depois da análise, produza um roadmap em fases.

Para **CADA fase** informe:

- Nome;
- Objetivo;
- Por que ela existe;
- Pré-requisitos;
- Camadas/projetos afetados;
- Principais alterações;
- Entidades envolvidas;
- Endpoints envolvidos;
- Alterações de frontend;
- Riscos;
- Testes necessários;
- Critério de conclusão;
- Se exige migration;
- Se exige mudança de configuração/deploy;
- Security Impact;
- Performance Impact.

Classifique cada fase:

```text
ESSENCIAL
IMPORTANTE
OPCIONAL / FUTURA
```

Explique dependências entre fases.

---

## 66. Ordem de implementação

Ao final, apresente uma ordem recomendada numerada.

Queremos responder principalmente:

```text
O que fazemos AGORA?

O que fazemos DEPOIS?

O que devemos deixar para mais tarde?
```

Considere explicitamente se a sequência deve começar por algo semelhante a:

1. limpeza/refactor controlado;
2. fundação PostgreSQL/EF Core;
3. PerfilRemetente;
4. segurança de credenciais;
5. seleção de remetente;
6. jobs persistentes;
7. concorrência por remetente;
8. histórico;
9. autenticação/autorização;
10. Docker/deploy.

**MAS NÃO ASSUMA QUE ESSA ORDEM É CORRETA.**

Analise o código e proponha a melhor ordem.

---

## 67. Decisões e alternativas

Sempre que houver mais de uma solução razoável:

- descreva as alternativas;
- vantagens;
- desvantagens;
- complexidade;
- impacto futuro;
- recomendação.

Exemplos:

```text
Channel vs SemaphoreSlim vs scheduler persistente

Data Protection vs outra estratégia para secrets

Polling vs SignalR

React Router agora vs depois

Uma tabela EnvioDestinatario vs somente FalhaEnvio
```

Não faça escolhas sem justificar.

---

## 68. Formato obrigatório da resposta

Organize a resposta nestas seções:

1. Resumo executivo
2. Arquitetura atual encontrada
3. Divergências entre código e prompt
4. Pontos fortes atuais
5. Dívidas e riscos
6. Arquitetura-alvo
7. Modelo de dados conceitual
8. Segurança das credenciais
9. Estratégia de processamento concorrente
10. Persistência e recuperação de jobs
11. Autenticação/autorização
12. Frontend e UX
13. Infraestrutura/deploy
14. Estratégia de testes
15. Roadmap por fases
16. Ordem recomendada
17. Decisões que precisam de aprovação humana
18. Threat model
19. Controles de segurança
20. Análise de capacidade e performance
21. Limites e backpressure
22. Cenários de falha e recuperação
23. Production readiness checklist
24. Riscos aceitos / riscos ainda não resolvidos

**NÃO altere arquivos.**

Ao final, pare e aguarde nossa aprovação do roadmap antes de implementar qualquer etapa.
