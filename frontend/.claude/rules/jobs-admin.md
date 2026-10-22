---
paths:
  - "src/pages/admin/jobs/**"
  - "src/i18n/pt-BR/jobs.json"
---

# Decisões — Tela de jobs (`/admin/jobs`)

Pedido do usuário em 21/10/2026: uma área de dev para o admin ver os jobs agendados, ler o erro quando algum falha e disparar um job na mão. Os casos dele são a notícia importante que acabou de sair e a primeira coleta de notícias depois do deploy. A regra e os endpoints estão em `../api/.claude/rules/jobs-hangfire.md` (seção "Tela de jobs").

## O que a tela mostra

- **Item "Jobs"** na seção Administração (`ActivityIcon`), com `permission: Permissions.systemJobsView`. Hoje só o Admin tem.
- **Aba "Agendados"**: uma linha por recorrente, com:
  - o nome do `[JobDisplayName]` e, embaixo, o id, a fila e o cron em `font-mono`, que é o que se procura no log;
  - a última execução: o badge de estado (`job-state.ts`, `Record` exaustivo dos estados do Hangfire, e estado desconhecido aparece cru com tom neutro), o tempo relativo com a data completa no `title`, e a duração;
  - a próxima execução;
  - o **Resultado** em `<pre>`, com o JSON formatado. É o que o job devolveu, e é por ele que se vê o `news:collect` bloqueado pelo Cloudflare (`"Blocked": true`) mesmo com o job "Concluído";
  - o **erro** numa caixa `destructive`, com o stack num `<details>` fechado.
- **"Rodar agora"** (`AppButton` neutro): só aparece com `systemJobsManage` e `canTrigger`, então o lembrete de evento fica sem botão. Fica desligado e mostra "Rodando..." enquanto o último estado é `Enqueued` ou `Processing`. O sucesso toasta `RECURRING_JOB_TRIGGERED` (`toastSuccess`), e o `JOB_ALREADY_RUNNING` sai pelo toast de erro global.
- **Atualização sozinha**: enquanto algum job está na fila ou rodando, a lista é buscada de novo a cada 3 s pelo `usePolling`, que já pausa com a aba escondida, sem toast de erro (`getRecurringJobs(quiet)`). Assim a pessoa clica, vê "Rodando" e depois "Concluído" com o resultado, sem recarregar. Sem nada rodando, não há polling; o botão "Atualizar" do cabeçalho busca na hora.
- **Aba "Falhas"**: a lista do Hangfire, paginada (`AppPagination` em `pages`), com o nome, o `#jobId`, o recorrente de origem, a fila, o tipo e a mensagem da exceção, e o stack num `<details>`. Sem falhas, mostra o `AppEmptyState` "Nenhuma falha". Só busca quando a aba abre.
- **Campos tolerados ausentes** (`name`, `lastResult`, `lastError`, `canTrigger`…): o front sobe antes da API no deploy (`DEPLOY.md`), e por um minuto a tela recebe o DTO antigo.

## Roteiro de verificação

Com a API no ar, logado como admin:

1. A sidebar mostra Administração → Jobs; uma conta comum não vê o item e, em `/admin/jobs`, cai no `/feed`.
2. "Agendados" lista os recorrentes com nome, fila, cron, última e próxima execução. O "Lembrar shows de amanhã" não tem botão.
3. "Rodar agora" no `news:collect` mostra o toast, o badge "Rodando" e depois "Concluído", com o Resultado (`Added`, `Blocked`) sem recarregar. Clicar de novo enquanto roda não é possível (botão desligado).
4. Com o LocalStack parado, "Rodar agora" no `media:cleanup` termina com a caixa de erro, e o job aparece na aba "Falhas" com o stack.
5. A 390px: sem rolagem horizontal, as três colunas da linha viram uma, e o botão fica embaixo do nome.
