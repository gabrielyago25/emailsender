# Limites da mensagem

`POST /api/envios` valida a mensagem antes de ler a planilha ou criar um job.
Os valores padrão são:

| Configuração | Padrão | Regra |
| --- | --- | --- |
| `LimitesMensagem:AssuntoMaximoCaracteres` | 200 | Comprimento da string recebida, antes de remover espaços das extremidades; usa unidades UTF-16, como o `maxLength` da interface |
| `LimitesMensagem:CorpoMaximoBytes` | 102400 | Tamanho em UTF-8, incluindo marcação HTML; aplicado antes e depois da sanitização |

Podem ser definidos na configuração local da API:

```json
{
  "LimitesMensagem": {
    "AssuntoMaximoCaracteres": 200,
    "CorpoMaximoBytes": 102400
  }
}
```

Ou por variáveis de ambiente `LimitesMensagem__AssuntoMaximoCaracteres` e
`LimitesMensagem__CorpoMaximoBytes`. Valores não positivos impedem a inicialização.
A interface permanece com limite de 200 caracteres no assunto; alterações desse
contrato devem considerar também o frontend.

O assunto não aceita caracteres de controle, incluindo quebras de linha. O corpo
precisa conter texto após a sanitização: marcação vazia, listas sem texto, espaços
e caracteres Unicode de formatação isolados não são suficientes. Violações
retornam HTTP 400 com `mensagem`, sem reproduzir o conteúdo recebido.

Somente o corpo sanitizado entra na fila. A sanitização no envio SMTP permanece
como defesa adicional. O limite se aplica ao corpo informado pelo usuário, não
ao tamanho MIME final acrescido da assinatura configurada no servidor.

Essas verificações ocorrem após o model binding. Não substituem limites de tamanho
da requisição, controle de uploads simultâneos, limites XLSX ou backpressure da
fila, previstos nas próximas entregas da fase 1.
