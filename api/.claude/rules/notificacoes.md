---
paths:
  - "**/NoiseVault.Application/Notifications/**"
  - "**/NoiseVault.Infra.Data/Repositories/Notifications/**"
  - "**/NoiseVault.Api/Controllers/NotificationsController.cs"
  - "**/NoiseVault.Api/Realtime/**"
  - "**/NoiseVault.Domain/Entities/Notification.cs"
  - "**/NoiseVault.Workers/**/Notifications/**"
---

# Decisões — Notificações

Módulo 6 da ordem em `dominio.md`. Decisões do usuário em 14/10/2026:
- **Gatilhos**: todos os grupos (curtida e comentário, eventos, mural, banda).
- **E-mail**: só no convite de banda, com opção de desligar.
- **Avisos em massa** (o Banner/Popup do legado) ficam para o painel de administração.
- **Tempo real**: SignalR.

## Modelo: dado estruturado, não texto

**`Notification`**, uma linha por destinatário:
- `UserId`, `Kind` (`ENotificationKind`);
- `ActorUserId?` (o último que agiu) e `ActorCount`;
- os alvos `PostId?`, `EventId?`, `ArchiveEntryId?`, `BandId?`, `BandInvitationId?`, `SupportTicketId?` (módulo 7, `suporte.md`);
- `CreatedAt`, `LastActivityAt` e `ReadAt?`.

Por que é assim:
- **Sem `Title`/`Message` gravados**, ao contrário do que `dominio.md` previa. O texto sai do i18n do front pelo `kind` (o mesmo contrato do `code`), e o título do evento ou o nome da banda é lido **ao vivo**: renomear um evento muda a notificação antiga.
- **Não herda `EntityBase`**, como `PostLike`: é volume alto e efêmero, sem soft delete nem trilha de auditoria. O repositório é independente (`INotificationRepository`), e a exclusão é física.
- **Alvo apagado some sozinho.** O repositório filtra a notificação cujo post, evento, registro ou banda foi apagado, ou cujo convite foi revogado. Por isso os `Delete` dos outros módulos não precisam de gancho. A purga diária remove essas linhas de vez.

## Quem recebe o quê

| `Kind` | Quem recebe | Agrupa | Gancho |
|---|---|---|---|
| `PostLiked` 0 | autor do post | sim | `PostAppService.ReactAsync`, só quando a curtida é nova |
| `PostCommented` 1 | autor do post | sim | `CommentAppService.Add` |
| `EventCommented` 2 | organizador | sim | `EventCommentAppService.Add` |
| `EventInterest` 3 | organizador | sim | `EventAppService.ReactAsync`, só no "Vou" novo |
| `EventCancelled` 4 | quem marcou "Vou" | não | `EventAppService.Cancel` |
| `EventTomorrow` 5 | quem marcou "Vou" (sem ator) | não | job `events:remind-tomorrow` |
| `EventLineup` 6 | integrantes da banda que **entrou** no line-up | não | `Create` e `Update` do evento (só as bandas novas em relação ao line-up anterior) |
| `ArchiveLiked` 7 | autor do registro | sim | `ArchiveAppService.ReactAsync` |
| `ArchiveCommented` 8 | autor do registro | sim | `ArchiveCommentAppService.Add` |
| `ArchiveAttended` 9 | autor do registro | sim | `ArchiveAppService.ReactAsync` |
| `ArchiveCommentedWhereAttended` 10 | quem marcou "Eu estava lá" (menos o autor) | sim | `ArchiveCommentAppService.Add` |
| `BandInvite` 11 | convidado (+ e-mail) | não | `BandInvitationAppService.Invite` |
| `BandInviteAccepted` 12 / `BandInviteDeclined` 13 | quem convidou | não | `Accept` / `Decline` |
| `BandPromoted` 14 / `BandRemoved` 15 | o integrante | não | `BandAppService.PromoteMember` / `RemoveMember` |
| `BandMemberLeft` 16 | os Owners que ficaram | não | `BandAppService.Leave` |
| `SupportTicketCreated` 17 | quem tem `Support.View` | não | `SupportAppService.Create` |
| `SupportTicketReplied` 18 | o outro lado da conversa (quem reportou, ou quem tem `Support.View`) | sim | `ReplyAsStaff` / `ReplyAsReporter` |
| `SupportTicketStatusChanged` 19 | quem reportou | não | `SupportAppService.Update`, só quando o status muda |
| `ArchiveSubmissionReceived` 20 | quem tem `Archive.Review` (menos quem enviou) | não | `ArchiveAppService.Submit` |
| `ArchiveSubmissionApproved` 21 | quem enviou | não | `ArchiveAppService.Approve` |
| `ArchiveSubmissionRejected` 22 | quem enviou | não | `ArchiveAppService.Reject` |
| `MentionedInPost` 23 · `MentionedInComment` 24 | a pessoa mencionada com @ (`marcacoes.md`) | não | `PostAppService.Create`/`Update` e os três `Add` de comentário, pelo `IMentionAppService` |
| `BandMentionedInPost` 25 · `BandMentionedInComment` 26 | os Owners da banda mencionada, com `BandId` | não | idem |
| `PhotoTagged` 27 | a pessoa marcada numa foto | não | `PhotoTagAppService.Add` |
| `BandOpeningInterest` 28 | os Owners da banda, quando alguém mostra interesse numa vaga (`ensaio.md`) | não | `BandOpeningAppService.ExpressInterest` |
| `BandVerificationRequested` 29 | quem tem `Bands.Verifications.View` (menos quem pediu), com `BandId`; mais o e-mail para `Email:VerificationInbox` por job | não | `BandVerificationAppService.Request` |
| `BandVerificationApproved` 30 · `BandVerificationRejected` 31 · `BandVerificationRevoked` 32 | os Owners da banda, com `BandId` (`bandas.md`) | não | `Approve` / `Reject` / `Unverify` |

**Regras gerais:**
- **Ninguém é notificado da própria ação**: o `NotifyMany` tira o ator da lista.
- **Agrupamento**: enquanto a linha do mesmo `(UserId, Kind, alvo)` não foi lida, a próxima interação atualiza essa linha em vez de criar outra. O ator passa a ser o último, `LastActivityAt` sobe, e `ActorCount` só cresce quando o ator muda. Isso vira "Fulano e mais 3 curtiram seu post". Depois de lida, a próxima interação abre uma linha nova.
  - A contagem é aproximada: se A, B e A comentarem, a frase diz "e mais 2".
  - Dois cliques simultâneos podem gerar duas linhas.
  - Aceito: é texto de aviso, não contador de verdade (o contador está no post).
- **Descurtir e desmarcar não apagam** a notificação.
- **Aceitar ou recusar um convite** marca como lida a notificação do convite. **Revogar** some com ela pelo filtro de visibilidade.

**Quem é dono do quê:** o `NotificationAppService` é o dono da entidade. Os outros AppServices chamam `Notify`/`NotifyMany` **depois** do commit. A falha vira `NOTIFICATION_FAILED` e nunca derruba a ação principal. Ele não depende de nenhum outro AppService, então não fecha ciclo de DI. O lembrete de "amanhã" mora no `EventAppService.RemindTomorrow`, porque quem sabe de evento é o dono do evento.

## Tempo real: SignalR só avisa

- **Hub**: `Api/Realtime/NotificationsHub.cs`, em `/hubs/notifications`, com `[Authorize]` e sem métodos de cliente.
  - O servidor manda `unread` com `{ unreadCount }` para o usuário (`IUserIdProvider` lê a claim `user_id`).
  - A lista continua no REST paginado. O socket nunca carrega dado de notificação.
- **`INotificationPusher`** (Application) é implementado por `SignalRNotificationPusher` (Api), registrado no `Program.cs`, porque o hub é da Api.
  - O Hangfire roda no mesmo processo, então o job de "amanhã" também empurra.
  - Falha no push vira `NOTIFICATION_PUSH_FAILED`; o front cobre com polling.
- **Token na query**: o navegador não manda header no WebSocket. O `OnMessageReceived` do JWT (`Infra.IoC/DependencyInjection.cs`) aceita `access_token` da query **só** em `/hubs`.
  - 🔴 O `RequestDescriptor` troca o valor por `***` antes de logar a `Query`.
  - É a segunda exceção registrada à regra "segredo nunca em URL" (a primeira é o e-mail do link de redefinição, `email.md`). O token vive 15 min, e o cliente renova antes de conectar.
- **Cliente só com WebSocket** (`skipNegotiation`): sem a fase de negociação, o balanceador não precisa de sticky session.
- **Keep-alive**: 15 s; timeout do cliente: 30 s.
- **Produção (AWS, Lightsail com Caddy na frente, `hospedagem.md`)**:
  - o Caddy repassa WebSocket sem configuração extra;
  - o keep-alive de 15 s sobra para qualquer idle timeout de proxy;
  - com **2 ou mais instâncias** da API, entra o backplane (`AddStackExchangeRedis`, ElastiCache), senão o push sai na instância errada. Com uma instância, não precisa.

## E-mail: só o convite de banda

- **`User.EmailOnBandInvite`** (padrão `true`), alterado por `PUT /api/profile/notification-settings` (`NOTIFICATION_SETTINGS_UPDATED`) e devolvido no `ProfileDto`.
- O convite enfileira `IEnqueuedJobDispatcher.EnqueueBandInviteEmail`. Na fila `notifications`, o `NotificationsEnqueuedJobsTrigger` chama `BandInvitationAppService.SendInviteEmail`.
  - Na hora de rodar, o job **relê** o convite e só envia se ele ainda estiver pendente e dentro da validade, e se a preferência estiver ligada.
  - Transporte recusou → exceção → retry (2 tentativas, 1 e 10 min).
- `IEmailService.SendBandInviteAsync`:
  - assunto **sem nome**: "Você recebeu um convite de banda";
  - corpo: quem convidou, a banda, o instrumento e a validade;
  - botão para `/notificacoes`.

## Retenção

O job `notifications:purge` (diário às 4 h) apaga as linhas com `LastActivityAt` acima de **90 dias** e as que perderam o alvo.

## Endpoints

| Método e rota | Code |
|---|---|
| `GET /api/notifications?unreadOnly=` (paginado, mais recentes primeiro por `LastActivityAt`) | `NOTIFICATIONS_FOUND` |
| `GET /api/notifications/unread-count` → `{ count }` | `NOTIFICATIONS_UNREAD_COUNT_FOUND` |
| `PUT /api/notifications/{id}/read` | `NOTIFICATION_READ` |
| `PUT /api/notifications/read-all` | `NOTIFICATIONS_ALL_READ` |
| `DELETE /api/notifications/{id}` | `NOTIFICATION_DELETED` |
| `PUT /api/profile/notification-settings { emailOnBandInvite }` | `NOTIFICATION_SETTINGS_UPDATED` |

- **Erro**: `NOTIFICATION_NOT_FOUND`, também para a notificação de outra pessoa. O filtro de dono fica no `WHERE`.
- **`NotificationDto`**:
  - `id`, `kind`, `actor` (`ProfileSummaryDto?`), `actorCount`;
  - `post { id, excerpt }` (80 caracteres), `event { id, title, startsAt }`, `archive { id, title }`;
  - `band` (`BandSummaryDto`), `invitation { id, status, instrument, expiresAt }`;
  - `ticket { id, protocol, title, status, mine }`: `mine` diz se o destinatário é quem reportou, e o front escolhe `/suporte/:id` ou `/admin/suporte/:id`;
  - `readAt`, `lastActivityAt`.
- **Limites**: sem política própria. Ler e marcar ficam no limite global, e quem cria notificação são ações que já têm o seu limite.

## Jobs

| Id | Fila | Quando | O que faz |
|---|---|---|---|
| `events:remind-tomorrow` | `notifications` | diário às 10 h (`AppTime.Zone`) | `EventTomorrow` para quem marcou "Vou" nos eventos publicados que começam amanhã (dia local) |
| `notifications:purge` | `notifications` | diário às 4 h | retenção de 90 dias e limpeza de alvo perdido |
| e-mail do convite (enfileirado) | `notifications` | ao convidar | `SendInviteEmail` |

## Eventos de log (contexto `Notifications`, salvo indicação)

| Evento | Nível | Dados |
|---|---|---|
| `NOTIFICATION_FAILED` | Error | `Kind`, `Recipients`, ids dos alvos |
| `NOTIFICATION_PUSH_FAILED` | Error | `Recipients` |
| `NOTIFICATIONS_PURGED` | Info | `Removed`, `RetentionDays` |
| `EVENT_REMINDERS_SENT` (contexto `Events`) | Info | `Events`, `Recipients` |
| `BAND_INVITE_EMAIL_SENT` / `BAND_INVITE_EMAIL_FAILED` / `BAND_INVITE_EMAIL_ENQUEUE_FAILED` (contexto `Bands`) | Info / Warning / Error | `InvitationId` |

Notificação criada não loga: é volume alto, e o dado está no banco.

## Roteiro de verificação

Com a API e o Mailpit no ar. Contas: A (admin, Owner da serra-negra), B (`guitarra073019`, Member) e C (fã). O script `notifications-api.mjs` vive fora do repositório e usa o `@microsoft/signalr` do `node_modules` do front.

1. **Hub.**
   - C conecta em `/hubs/notifications` com o token. Sem token → 401.
   - Um 5xx forçado com `?access_token=x` na query aparece no log com `***`.
2. **Curtida.**
   - A curte o post de C → C recebe `unread` pelo hub, e a lista traz `PostLiked` com `actorCount: 1`.
   - B curte → continua uma linha, com `actorCount: 2`.
   - Curtir de novo não muda nada; curtir o próprio post não notifica.
3. **Evento.**
   - C comenta e marca "Vou" no evento de A → `EventCommented` e `EventInterest`.
   - A cancela → C recebe `EventCancelled`.
   - A cria um evento com a serra-negra no line-up → B recebe `EventLineup`, e A, o ator, não recebe.
4. **Mural.** C marca "Eu estava lá" → A recebe `ArchiveAttended`. B comenta → A recebe `ArchiveCommented`, e C recebe `ArchiveCommentedWhereAttended`.
5. **Banda.**
   - A convida C → `BandInvite` e e-mail no Mailpit com o assunto sem nome. Com `emailOnBandInvite = false`, sem e-mail.
   - C recusa → A recebe `BandInviteDeclined`, e a notificação do convite de C fica lida.
   - Um convite revogado some da lista de C.
   - Promover, remover e sair notificam quem a tabela diz.
6. **Dono.**
   - C marca como lida ou apaga uma notificação de A → 404.
   - `read-all` zera o `unread-count`.
   - Apagar o post tira as notificações dele da lista.
7. **Jobs.**
   - `events:remind-tomorrow` disparado no `/hangfire` com um evento amanhã → `EventTomorrow` sem ator.
   - Uma linha com `LastActivityAt` de 91 dias some no `notifications:purge`.
