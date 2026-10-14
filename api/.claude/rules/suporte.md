---
paths:
  - "**/NoiseVault.Application/Support/**"
  - "**/NoiseVault.Infra.Data/Repositories/Support/**"
  - "**/NoiseVault.Api/Controllers/SupportController.cs"
  - "**/NoiseVault.Api/Controllers/Admin/SupportAdminController.cs"
  - "**/NoiseVault.Domain/Entities/SupportTicket*.cs"
  - "**/NoiseVault.Domain/Enums/Support/**"
---

# Decisões — Suporte

Módulo 7 da ordem em `dominio.md`, **redefinido pelo usuário em 14/10/2026**. O previsto era o `IssueReport` do legado, que virava issue no GitHub por um job. O desenho caiu:
- o projeto tem uma pessoa só, sem equipe de suporte, e tudo num ponto único é mais fácil de acompanhar do que app e GitHub separados;
- o relato passa a ser um **ticket no banco**, com uma **tela de administração** no próprio Noise Vault e uma **conversa** com quem reportou;
- **GitHub fica de fora "por enquanto"**. Se voltar, entra como ação de admin sobre um ticket já triado (um botão "abrir issue"), não no caminho do envio.

## Modelo

- **`SupportTicket`** (`EntityBase`):
  - `ReporterUserId`, `Title` (5–120), `Description` (20–5.000);
  - `Status` (`ESupportTicketStatus`: `Open` 0, `InProgress` 1, `Resolved` 2, `Dismissed` 3) e `Priority` (`ESupportTicketPriority`: `Low` 0, `Normal` 1, `High` 2; nasce `Normal`);
  - **contexto capturado pelo front**, que a pessoa não preenche: `OriginPath?` (a rota de onde ela veio, até 300, só o caminho, sem query string), `UserAgent?` (até 500), `ViewportWidth?` e `ViewportHeight?`;
  - `InternalNote?` (até 5.000): anotação só da equipe, nunca sai para quem reportou;
  - `LastActivityAt` (abertura, mensagem ou mudança de status) e `ClosedAt?` (quando foi para `Resolved`/`Dismissed`).
- **Protocolo é o id**: `NV-0042` (`SupportRules.Protocol`, quatro dígitos com zero à esquerda, cresce sem cortar). Nada de coluna nova nem sequência própria: o id já é único, crescente e não reaproveitado.
- **`SupportTicketTag`** (`SupportTicketId`, `Tag`), chave composta, **pelo menos uma**. `ESupportTag`: `Bug` 0, `Suggestion` 1, `Account` 2, `Mobile` 3, `Desktop` 4. As tags do legado (Bug, Sugestão, Celular, Desktop) mais Conta. O front pré-marca Celular ou Desktop pela largura da tela; quem decide é a pessoa.
- **`SupportTicketArea`** (`SupportTicketId`, `Area`), **opcional**, várias. `ESupportArea`: `Feed` 0, `Events` 1, `Archive` 2, `Profile` 3, `Bands` 4, `Notifications` 5, `Account` 6, `SignIn` 7, `Other` 8. Decisão do usuário: o bug nem sempre está na última página que a pessoa abriu, então o `OriginPath` sozinho engana. Os dois convivem: a área é o que a pessoa diz; o caminho é o que o navegador viu.
- **`SupportTicketMedia`** (`SupportTicketId`, `MediaUploadId` único, `Order`): até **6 mídias**, das quais **no máximo 1 vídeo**. A navegação `MediaUpload.SupportAttachment` é o vínculo "em uso", como `Attachment` e `ArchiveAttachment`.
- **`SupportTicketMessage`** (`EntityBase`): `SupportTicketId`, `AuthorUserId`, `Text` (1–2.000), `FromStaff`. A conversa entre a equipe e quem reportou; só texto.
- O "não procede" é `Dismissed`, não exclusão: ticket não é apagado, nem pela equipe. Spam fica arquivado como `Dismissed`, com a nota dizendo por quê.
- Tags e áreas em tabela de ligação, nunca `List<Enum>` em coluna (`dominio.md`). Valor novo só no fim.

## Mídia: perfil `Support`

Mesmo pipeline do feed (foto pela API, vídeo em multipart direto ao S3, HLS no job), com o perfil `EMediaProfile.Support` = 2 (`midia-e-video.md`):
- foto: preset `Post` (15 MB de entrada, lado maior até 2048), suficiente para print de tela;
- vídeo: **200 MB e 2 min** (decisão do usuário: gravação de tela curta mostrando o bug), prazo de processamento de 2 h.

Upload por `POST /api/support/media/photos|videos`; concluir, consultar e apagar reaproveitam `/api/media`. Mídia de outro perfil não entra no ticket (`MEDIA_NOT_ATTACHABLE`).

O ticket **é aberto mesmo com vídeo ainda processando**: a pessoa não fica esperando a conversão para mandar o relato, e a tela do admin mostra "processando" até ficar pronto.

**Risco aceito em 14/10/2026**: a mídia do ticket fica no mesmo bucket das outras, com URL pública fixa e GUID na chave (`armazenamento.md`). Só quem reportou e a equipe recebem a URL, mas um print pode mostrar dado pessoal (a `/minha-conta` mostra o e-mail). Se isso virar problema, os anexos de suporte passam a ter URL assinada de curta duração: muda só o `Describe` da mídia desse perfil.

## Quem pode o quê

| Ação | Quem |
|---|---|
| Abrir ticket, subir mídia de suporte | qualquer pessoa logada |
| Ver e responder os **próprios** tickets | quem reportou (filtro `ReporterUserId` no `WHERE`) |
| Ver a caixa de entrada, qualquer ticket, mensagens, nota interna | `Support.View` |
| Mudar status, prioridade e nota interna | `Support.Edit` |
| Responder como equipe | `Support.Reply` |

- Três verbos porque são três ações de escrita (`CLAUDE.md`: `Manage` só com uma). `Edit` e `Reply` declaram `View` como pai. Nenhum papel padrão recebe; o `Admin` tem todas.
- Ticket de outra pessoa pelo endpoint do usuário → `SUPPORT_TICKET_NOT_FOUND`, nunca 403: não confirma que o id existe.

## Ciclo do ticket

- Nasce `Open`.
- A **primeira resposta da equipe** num ticket `Open` passa para `InProgress`: responder já é estar olhando.
- `Support.Edit` muda para qualquer status. Ir para `Resolved`/`Dismissed` grava `ClosedAt`; voltar para `Open`/`InProgress` zera.
- **Quem reportou responde** em `Open`, `InProgress` e `Resolved`. Responder um `Resolved` **reabre** como `Open` ("ainda está acontecendo"). Em `Dismissed` a conversa fecha (`SUPPORT_TICKET_CLOSED`).
- Transição de status é `UPDATE` comum: quem muda é uma pessoa, uma tela; não há corrida que mereça guarda no `WHERE`.

## Limites contra abuso

- Taxa: `support-create` 5/h por conta (`abuso-e-limites.md`). Mensagens usam `comment-create`; uploads, `media-photo` e `media-video`.
- Volume: no máximo **10 tickets abertos** (`Open`/`InProgress`) por pessoa (`SUPPORT_TICKET_LIMIT_REACHED`). Quem tem 10 relatos sem resposta não precisa de um 11º; precisa que a equipe responda.

## Notificações

Três `ENotificationKind` novos, com o alvo `Notification.SupportTicketId` (`notificacoes.md`):

| `Kind` | Quem recebe | Agrupa | Gancho |
|---|---|---|---|
| `SupportTicketCreated` 17 | quem tem `Support.View` (menos quem abriu) | não | `SupportAppService.Create` |
| `SupportTicketReplied` 18 | resposta da equipe → quem reportou; resposta de quem reportou → quem tem `Support.View` | sim | `Reply` dos dois lados |
| `SupportTicketStatusChanged` 19 | quem reportou | não | `SupportAppService.Update`, só quando o status muda |

- Um AppService só (`SupportAppService`) é dono do ticket, das mensagens e das duas superfícies, como o `ArchiveAppService` com o `ArchiveAdminController`: os métodos de equipe só existem atrás das policies do `SupportAdminController`.
- "Quem tem `Support.View`" sai de `IPermissionChecker.UsersGrantedAsync`: papel `Admin`, papéis com a claim e usuários com a claim. É lista curta (a equipe), lida a cada ticket.
- O `NotificationDto.ticket` traz `{ id, protocol, title, status, mine }`. `mine` diz se o destinatário é quem reportou: o front leva para `/suporte/:id` ou `/admin/suporte/:id`. O título é lido ao vivo, como os outros alvos.
- Sem e-mail: a conversa toda acontece no Noise Vault. O texto do mock ("a gente responde pelo seu e-mail") morre.

## Leitura

- **Do usuário**: `GET /api/support/tickets` (os próprios, paginado, por `LastActivityAt` desc) e `GET /api/support/tickets/{id}`. Sem nota interna e sem o bloco de contexto (ele já sabe de onde veio).
- **Da equipe**: `GET /api/admin/support/tickets`, paginado, com os filtros:
  - `status`, `tag`, `area`, `priority`;
  - `search`: título, descrição, nome ou nickname de quem reportou; `NV-42` ou `42` acha pelo protocolo;
  - `sort`: `activity` (padrão, `LastActivityAt` desc), `newest`, `priority` (Alta primeiro, depois atividade). Sempre com desempate por `Id`.
- `GET /api/admin/support/summary` → `{ open, inProgress, resolved, dismissed }`: a contagem das abas, num `GroupBy` só.
- Cada item da lista traz `messageCount` e `awaitingStaff`: a última mensagem é de quem reportou, ou não há mensagem e o ticket está `Open`. É o "falta você responder" da caixa de entrada.
- As mensagens são paginadas (`GET …/{id}/messages`, mais antigas primeiro), nos dois lados.

## Endpoints

| Método e rota | Code de sucesso |
|---|---|
| `POST /api/support/tickets` | `SUPPORT_TICKET_CREATED` |
| `GET /api/support/tickets` · `GET /api/admin/support/tickets` (`Support.View`) | `SUPPORT_TICKETS_FOUND` |
| `GET /api/support/tickets/{id}` · `GET /api/admin/support/tickets/{id}` (`Support.View`) | `SUPPORT_TICKET_FOUND` |
| `GET …/tickets/{id}/messages` (nos dois) | `SUPPORT_MESSAGES_FOUND` |
| `POST /api/support/tickets/{id}/messages` · `POST /api/admin/support/tickets/{id}/messages` (`Support.Reply`) | `SUPPORT_MESSAGE_CREATED` |
| `PUT /api/admin/support/tickets/{id}` `{ status, priority, internalNote }` (`Support.Edit`) | `SUPPORT_TICKET_UPDATED` |
| `GET /api/admin/support/summary` (`Support.View`) | `SUPPORT_SUMMARY_FOUND` |
| `POST /api/support/media/photos` · `…/videos` | `PHOTO_UPLOADED` / `VIDEO_UPLOAD_STARTED` |

**Erros:**
- `SUPPORT_TICKET_NOT_FOUND`, `SUPPORT_TICKET_CLOSED`, `SUPPORT_TICKET_LIMIT_REACHED`.
- Validação: `INVALID_SUPPORT_TITLE`, `INVALID_SUPPORT_DESCRIPTION`, `SUPPORT_TAG_REQUIRED`, `INVALID_SUPPORT_TAG`, `INVALID_SUPPORT_AREA`, `TOO_MANY_SUPPORT_MEDIA`, `TOO_MANY_SUPPORT_VIDEOS`, `INVALID_SUPPORT_MESSAGE`, `INVALID_SUPPORT_NOTE`, `INVALID_SUPPORT_STATUS`, `INVALID_SUPPORT_PRIORITY`.
- Reaproveitados: `MEDIA_NOT_ATTACHABLE`, `IMAGE_TOO_LARGE`, `VIDEO_TOO_LARGE`, `VIDEO_UNSUPPORTED`.
- O contexto (`originPath`, `userAgent`, viewport) é **cortado, não recusado**: texto longo demais é truncado e número fora de 1–20.000 vira nulo. Contexto ruim não pode impedir o relato de um bug.

## Eventos de log (contexto `Support`)

| Evento | Nível | Dados |
|---|---|---|
| `SUPPORT_TICKET_CREATED` | Info | `TicketId`, `Tags`, `Areas`, `Media` |
| `SUPPORT_TICKET_UPDATED` | Info | `TicketId`, `From`, `To`, `Priority` |
| `SUPPORT_TICKET_REOPENED` | Info | `TicketId` (resposta de quem reportou num `Resolved`) |

Mensagem não loga: o dado está no banco. Título, descrição, nota e texto de mensagem nunca entram no log: são texto livre de usuário.

## Roteiro de verificação

Com a API e o ffmpeg no ar; conta A (admin) e conta C (fã). O script `support-api.mjs` vive fora do repositório.

Contas novas são registradas e confirmadas pelo próprio script (lendo o link no Mailpit); o `register` tem 5/h por IP, então rodar o script mais de duas vezes na mesma hora pede reiniciar a API (o limitador é em memória). Tentativa recusada por validação também gasta o `support-create`.

1. **Permissão.** C → `GET /api/admin/support/tickets` → 403. A consegue.
2. **Abrir.**
   - C sobe um print e um vídeo de 30 s por `/api/support/media` e abre o ticket com tags Bug + Celular, áreas Feed + Eventos e o contexto → `SUPPORT_TICKET_CREATED` com `protocol` `NV-00xx`; A recebe `SupportTicketCreated` pelo hub.
   - Sem tag → `SUPPORT_TAG_REQUIRED`; dois vídeos → `TOO_MANY_SUPPORT_VIDEOS`; mídia do feed → `MEDIA_NOT_ATTACHABLE`; `userAgent` de 2.000 caracteres → aceito e cortado em 500; `originPath` com query → só o caminho; viewport de 99.999 → nulo.
   - Vídeo de 3 min → a mídia termina `Failed` com `TOO_LONG`; start de 300 MB → `VIDEO_TOO_LARGE`.
3. **Caixa de entrada.** O ticket aparece em `status=0` com `awaitingStaff: true`; `summary` conta 1 em `open`; `search=NV-xx` e `tag=0` acham.
4. **Conversa.**
   - A responde → status `InProgress`, C recebe `SupportTicketReplied` com `ticket.mine: true`.
   - C responde → A recebe `SupportTicketReplied` com `mine: false`, e `awaitingStaff` volta a `true`.
5. **Status.** A marca `Resolved` com nota → C recebe `SupportTicketStatusChanged`; o `GET` de C não traz a nota. C responde → volta para `Open` com `SUPPORT_TICKET_REOPENED` no log. A marca `Dismissed` → resposta de C → `SUPPORT_TICKET_CLOSED`.
6. **Dono.** Outra conta lê o ticket de C → `SUPPORT_TICKET_NOT_FOUND`.
7. **Volume.** O 11º ticket aberto de C → `SUPPORT_TICKET_LIMIT_REACHED`; o 6º em uma hora → 429.
8. **Limpeza.** Com `ExpiresAt` das mídias do ticket e de mídias soltas no passado, o `media:cleanup` apaga só as soltas (`MEDIA_CLEANED` com `Unattached`); as do ticket continuam `Ready`, com chave.

Resultado em 14/10/2026: todos os passos passaram (ticket `NV-0013`, script `support-api.mjs` com 53 checagens, mais a limpeza feita à mão).
