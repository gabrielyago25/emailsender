# Testes do frontend

Na pasta `frontend`:

```bash
npm ci
npm test
npm run lint
npm run build
```

Os testes usam Vitest, Testing Library e jsdom. As chamadas à API são simuladas;
não é necessário iniciar o backend nem configurar SMTP para executá-los.

## Troca de planilhas

Os testes de `NovoEnvioPage` controlam a ordem das respostas para verificar que:

- selecionar outro arquivo cancela a requisição anterior;
- respostas e erros antigos não encerram o carregamento da seleção atual;
- respostas antigas não alteram uma revisão já aberta;
- a confirmação envia o arquivo correspondente aos totais revisados;
- falhas atuais são exibidas e permitem tentar novamente;
- desmontar a página cancela a validação pendente.

O teste do serviço verifica que o sinal de cancelamento chega ao `fetch`.
O cancelamento no navegador não garante a interrupção do parsing XLSX no servidor.
Os limites de processamento do backend serão tratados separadamente no roadmap.

Para conferir o comportamento no navegador, use a limitação de rede nas ferramentas
de desenvolvimento, selecione duas planilhas diferentes em sequência e confira os
totais e o nome da segunda na revisão. Após uma falha de rede, selecione o mesmo
arquivo novamente. Essa verificação complementa os testes em DOM simulado.

## Acompanhamento

Os testes de `AcompanhamentoEnvio` verificam o botão de novo envio nos estados
`Concluido`, `Falhou` e `Cancelado`, a exibição textual do erro geral (`erro`), a
remoção do progresso temporário nos estados finais e a interrupção do polling.
Também verificam a recuperação de uma falha temporária de consulta sem considerar
o job finalizado por causa do erro de rede.
