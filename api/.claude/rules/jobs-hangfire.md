---
paths:
  - "**/NoiseVault.Workers/**"
  - "**/NoiseVault.Infra.IoC/DependencyInjectionJobs.cs"
  - "**/NoiseVault.Application/BackgroundJobs/**"
---

# Convenção — Background jobs (Hangfire)

Hangfire com storage no **mesmo banco** do EF (`DefaultConnection`; Postgres ou SQL Server, decidido em `Program.cs` por `Database:Provider`), rodando no processo da API. Dashboard em `/hangfire`.

🔴 **O dashboard só é mapeado fora de `Production`** (`Program.cs`), e o que é ambiental é a **interface web** — workers e jobs rodam em todos os ambientes. O `HangfireDashboardAuthorizationFilter` autoriza tudo porque a barreira, quando existe, fica na infraestrutura (proxy autenticado). Expor `/hangfire` em produção sem barreira é bug.

Todo o código de jobs vive em `src/NoiseVault.Workers`, com **duas categorias separadas**.

## 0. Filas: nomeadas por domínio

| Fila | Servidor | Quem roda ali |
|---|---|---|
| `notifications` | `noisevault-main` | e-mail do convite de banda, `events:remind-tomorrow` e `notifications:purge` (`notificacoes.md`). Vem antes de `default` na lista: é a prioridade. |
| `default` | `noisevault-main` | `discography:refresh`, `news:collect` e `news:purge`: jobs leves de integração e limpeza que não são notificação nem mídia. Também está na lista para um job sem `[Queue]` não ficar invisível. |
| `media` | `noisevault-media` (`WorkerCount = 2`) | conversão de vídeo, `media:cleanup` e `media:transcode-poll`. Servidor próprio porque ffmpeg é CPU pesada: no máximo 2 conversões ao mesmo tempo, sem ocupar os workers do principal. |

🔴 **`[Queue]` é obrigatório em todo método de job.** O Hangfire enfileira em `default` qualquer método **sem** o atributo; se `default` não estiver na lista de nenhum servidor, o job fica `Enqueued` para sempre, sem erro, sem log. É a primeira coisa a conferir num job novo.

Fila nova exige acrescentá-la em `options.Queues` (ou num segundo `AddHangfireServer`) **no mesmo commit**. A ordem em `Queues` é prioridade. Um segundo servidor só se justifica para isolar trabalho pesado (PDF, importação) do resto — um único servidor drena todas as filas com o mesmo pool de workers.

## 1. Jobs recorrentes (RecurringJobs)

Padrão **Wrapper + Trigger**, espelhado por domínio:

```
src/NoiseVault.Workers/
  Wrappers/Recurring/{Domínio}/{Domínio}RecurringJobsWrapper.cs   ← registra o job (cron)
  Triggers/Recurring/{Domínio}/{Domínio}RecurringJobsTrigger.cs   ← método executado
```

Referência: `Wrappers/Recurring/Media/MediaRecurringJobsWrapper.cs` + `Triggers/Recurring/Media/MediaRecurringJobsTrigger.cs`. Enfileirado: `Triggers/Enqueued/Media/MediaEnqueuedJobsTrigger.cs`, disparado por `IEnqueuedJobDispatcher.EnqueueVideoTranscode`.

- `recurringJobId` no formato `"<recurso>:<ação>"` (`items:archive-expired`).
- `[Queue]` e `[JobDisplayName]` são **obrigatórios**. Sem display name o dashboard mostra `Classe.Método`.
- Cron via helpers `Cron.*` (não usar string cron crua).
- O Trigger **não contém lógica de negócio** — só injeta e chama o AppService, que devolve tipo cru (nenhum endpoint o chama).

### 🔴 Não existe trio padrão de atributos para copiar

Copiar `AutomaticRetry(3)` + `DisableConcurrentExecution(300)` em todo job produz jobs com perfis de falha diferentes carregando a mesma política porque ninguém pensou nela. **Job novo escolhe seus próprios valores**, e o porquê é registrado aqui — não em comentário no código.

| Job | Fila | Attempts / delays | Lock wait | Motivo |
|---|---|---|---|---|
| `media:cleanup` (recorrente, de hora em hora) | `media` | `2 · 60, 600` | 30s | varredura de conjunto idempotente; a próxima hora pega o que sobrar |
| `events:remind-tomorrow` (recorrente, diário às 10 h) | `notifications` | `0` | 30s | sem retry: repetir mandaria o lembrete duas vezes para quem já recebeu; se falhar, o log diz e o dia seguinte segue normal |
| `notifications:purge` (recorrente, diário às 4 h) | `notifications` | `2 · 60, 600` | 30s | `DELETE` idempotente; repetir não faz mal |
| `SendBandInviteEmailAsync(invitationId)` (enfileirado) | `notifications` | `2 · 60, 600`, `OnAttemptsExceeded = Fail` | — | SMTP fora do ar é transitório; o job relê o convite a cada tentativa, então não manda e-mail de convite já respondido |
| `SendBandVerificationEmailAsync(requestId)` (enfileirado) | `notifications` | `2 · 60, 600`, `OnAttemptsExceeded = Fail` | — | mesmo desenho do convite; o job relê o pedido e só manda se ainda estiver pendente |
| `discography:refresh` (`DiscographyRecurringJobsTrigger.RefreshAsync`) | `default` | `2 · 60, 600`, `DisableConcurrentExecution(60)` | diário às 5 h | reimporta do Spotify as bandas com sync de mais de 24 h, uma por vez com pausa de 1 s; para no primeiro 429 e tenta amanhã (`discografia.md`) |
| `news:collect` (recorrente, de hora em hora aos 15 min) | `default` | `0`, `DisableConcurrentExecution(30)` | 30s | sem retry: a hora seguinte refaz a busca inteira, e repetir logo depois de um bloqueio do Cloudflare só piora a nota do IP; uma matéria por vez com pausa de 1 s, no máximo 40 por execução (`noticias.md`) |
| `news:purge` (recorrente, diário às 4 h) | `default` | `2 · 60, 600` | 30s | `DELETE` idempotente das notícias com mais de 30 dias; repetir não faz mal |
| `media:transcode-poll` (recorrente, a cada minuto; só existe com `Video:Engine=MediaConvert`, senão o wrapper o remove) | `media` | `0`, `DisableConcurrentExecution(15)` | 15s | consulta `GetJob` das mídias em processamento e publica os posts das que ficaram prontas; sem retry porque o minuto seguinte repete tudo; sobreposição seria duas consultas iguais (`midia-e-video.md`) |
| `TranscodeVideoAsync(mediaId)` (enfileirado) | `media` | `2 · 60, 300`, `OnAttemptsExceeded = Fail` | — | falha transitória (S3, ffmpeg) merece nova tentativa; a **última** marca a mídia `Failed` em vez de relançar (o trigger lê `RetryCount` do `PerformContext`), senão o post ficaria "processando" para sempre. Rejeição definitiva (não é vídeo, longo demais) não relança. |

⚠️ **`DisableConcurrentExecution(timeoutInSeconds)` é o tempo de espera pelo lock distribuído, não um teto de duração.** Para job periódico o certo é estourar rápido (15–30s) e deixar o `[AutomaticRetry]` reagendar.

O critério entre recorrente e `BackgroundService`: **cadência de segundos com I/O por item é `BackgroundService`; varredura periódica de conjunto é recorrente.** O recorrente dá dashboard, retry e lock distribuído de graça.

### 🔴 Cron sem `TimeZone` é UTC

`Cron.Daily(hour: 5)` sozinho agenda **05:00 UTC**. Todo recorrente registra com `RecurringJobOptions { TimeZone = AppTime.Zone }`. O dashboard continua **exibindo em UTC**: ver diferença ali é sinal de que o `TimeZone` pegou.

### ⚠️ Recorrente declara a fila **duas** vezes, e a tela lê a certa

O `AddOrUpdate` leva `queue:` **além** do `[Queue]` no método, com o mesmo valor. O hash do recurring job guarda o `"Queue"` dentro do `Job` (que enfileira) e um campo `Queue` legado (sempre `default`). `RecurringJobDto.Queue` da API de storage devolve o **legado** — por isso `RecurringJobReader` lê `job.Job?.Queue ?? job.Queue`.

Registro de um novo recorrente (dois passos, ambos em `Infra.IoC/`):
1. `DependencyInjection.cs` (`AddInfra`, seção `// Jobs`): `AddScoped` do Trigger e do Wrapper.
2. `DependencyInjectionJobs.cs` (`RegisterJobs`): resolver o Wrapper e chamar `.Register()`.

## 2. Jobs disparados por uma ação (Enqueued)

```
src/NoiseVault.Workers/
  Dispatch/EnqueuedJobDispatcher.cs                              ← implementa a abstração de disparo
  Triggers/Enqueued/{Domínio}/{Domínio}EnqueuedJobsTrigger.cs    ← método executado
```

🔴 **O AppService não chama `BackgroundJob.Enqueue` direto — ele não consegue.** `Application` não referencia Hangfire, e referenciar `Workers` inverteria a dependência. O disparo passa por `IEnqueuedJobDispatcher` (`Application/Abstractions/Jobs/`), implementado em `Workers/Dispatch/`. Mesma forma do `IRecurringJobReader`.

- Não há Wrapper — nada a registrar no startup.
- Trigger injeta o AppService (ou o logger) e delega.
- Parâmetros do job devem ser serializáveis (ids, primitivos) — nunca entidades.

### Um método por fila e por nome, e o dispatcher roteia por tipo

Fila e display name são atributos de **método**. `IEnqueuedJobDispatcher.EnqueueBackgroundJobRun(runId, jobType)` escolhe o método por `switch`, e o arm `_` **lança**: tipo novo não mapeado deve estourar no request de quem disparou, com a linha ainda `Pending` e visível. **Valor novo em `EBackgroundJobType` obriga escolher a fila no dispatcher.** Isso não contradiz o "handler por DI" abaixo: aqui é *em que fila*, lá é *qual lógica*.

### `[AutomaticRetry(Attempts = 0)]` quando o job tem linha de status própria

🔴 **Job rastreado em `BackgroundJobRun` não usa o retry do Hangfire.** O status da linha **é** o modelo de retry, e os dois se contradizem: a 1ª tentativa faz o claim (`Pending → Running`), falha, grava `Failed`; a 2ª chama `TryClaimAsync`, que exige `Pending`, devolve false e não faz nada — e o Hangfire marca `Succeeded` enquanto a linha diz `Failed`.

Quem re-executa é o operador, por `POST /api/admin/jobs/runs/{id}/retry` (`System.Jobs.Manage`), que cria uma linha **nova** com o mesmo payload; a anterior fica como histórico. `[DisableConcurrentExecution]` não se aplica: a exclusão que importa é a `DeduplicationKey` (única filtrada, em voo) mais o `TryClaimAsync` atômico.

### Despacho por tipo: handler resolvido por DI, não `switch`

`BackgroundJobAppService.ExecuteAsync` injeta `IEnumerable<IBackgroundJobHandler>` e acha o handler por `JobType`. Tipo novo = uma classe em `Application/BackgroundJobs/Handlers/` mais uma linha de DI. A orquestração (claim, transição, captura de erro, `ResultJson`, liberação da dedup key) mora no `BackgroundJobAppService` — não no Trigger nem nos handlers. `ExecuteAsync` **não relança**: a falha já está na linha e no log (`BACKGROUND_JOB_FAILED`).

⚠️ Crash do processo **antes** do claim commitar deixa a linha em `Pending` para sempre — visível na tela, sem reaper. Se um projeto precisar, o molde é um recorrente que devolve `Running` antigos para `Failed`.

⚠️ **Job não gera auditoria global.** O interceptor só grava com usuário autenticado de papel diferente de `User`, e job não tem `HttpContext`. `BackgroundJobRun` está em `AuditPolicy.Ignored` porque **ela é** a trilha.
