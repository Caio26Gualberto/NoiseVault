---
paths:
  - "**/Headbanger.Domain/Entities/**"
  - "**/Headbanger.Domain/Enums/**"
---

# Decisões — Domínio da comunidade

O que o Headbanger é, em entidades, e em que ordem elas nascem. Nesta entrega **só `User` existe no código**; o resto está aqui para o Auth não nascer errado e para cada módulo futuro saber onde encaixa. Decisão de modelagem registrada antes do código é a spec do módulo.

## `User` é sempre uma pessoa; banda não tem login

No legado, uma banda era um `User` disfarçado: registro próprio, e-mail próprio, linha em `BandsUsers` com `IsBand = true` apontando para si mesma. Integrantes ficavam duplicados (JSON em `Band.Integrants` **e** linhas em `BandsUsers`), o instrumento morava no `User` (quem toca em duas bandas só podia ter um), o papel `Band` era calculado na hora do token e convites eram notificações com o id da banda dentro de uma string. Nada disso volta.

- **`User`** (`Name`, `Nickname`, `Email`, `IsActive`) é a conta de uma pessoa. Dados de perfil que não são identidade (avatar, banner, cidade, instrumento, gêneros, links) entram no módulo **Perfil**, na própria entidade ou numa `UserProfile` 1:1 — decidir quando o módulo nascer, com base no que o feed precisa carregar por post.
- **`Band`** (`Name`, `Slug`, `Description`, `City`, `Genres`, `FoundedAt?`, links, `IsVerified`, `AvatarKey?`, `BannerKey?`) é entidade própria, **sem conta**. Quem age em nome da banda é um `BandMember` com `Role = Owner`. URL pública `/bandas/:slug`; `Slug` único e imutável depois de publicado (renomear a banda não quebra link).
- **`BandMember`** (`BandId`, `UserId`, `Instrument`, `Role: Owner | Member`, `JoinedAt`): o instrumento é **da relação**, não da pessoa. `(BandId, UserId)` único. Uma banda tem pelo menos um `Owner`; o último `Owner` não sai sem passar o bastão.
- **`BandInvitation`** (`BandId`, `InvitedUserId`, `InvitedByUserId`, `Instrument`, `Status: Pending | Accepted | Declined | Revoked`, `ExpiresAt`): entidade real, com estado e validade. A notificação do convite é **projeção** desta linha, nunca o armazenamento. Aceitar = transição atômica `Pending → Accepted` no `WHERE` + criação do `BandMember` na mesma transação.
- **Verificação de banda** (`BandVerificationRequest` no legado) vira um campo `IsVerified` + uma ação de admin com permissão (`Bands.Verify`); o formulário com telefone e mensagem é um pedido que entra na fila de moderação, não uma entidade 1:1 colada na banda.

## Conteúdo: quem publica é sempre um `User`

`Post`, `Photo`, `Video`, `Event` e `Comment` têm `AuthorUserId` (FK `Restrict`). Conteúdo "da banda" é conteúdo de um membro **marcado** com `BandId` opcional — é assim que o perfil da banda lista posts sem a banda ter conta.

- **`Post`** (`Content`, `BandId?`) com `PostMedia` (ordem, tipo foto/vídeo, `StorageKey`, dimensões). Likes em **`PostLike (PostId, UserId)`** com índice único — nada de `Like` polimórfico sem FK como no legado. Comentário tem **um** pai: `Comment.PostId` obrigatório. Se evento ou foto ganharem comentários, cada um ganha a sua tabela (`EventComment`), não quatro FKs nulas na mesma.
- **`Photo`** (galeria) e **`Video`** (YouTube, só `YoutubeId` + título/descrição) passam por **moderação**: `ModerationStatus: Pending | Approved | Rejected`, `ModeratedByUserId?`, `ModeratedAt?`, `RejectionReason?`. A fila de moderação é `WHERE Status = Pending`, e aprovar/rejeitar é `ExecuteUpdateAsync` condicional. Rejeitar **registra o motivo** — o legado descartava.
- **`Event`** (`Title`, `Description`, `StartsAt`, `EndsAt?`, `Venue`, `City`, `Genres`, `TicketsUrl?`, `Price?`, `Capacity?`, `BannerKey?`, `BannerPosition?`, `Status: Draft | Published | Cancelled`) com `EventBand (EventId, BandId)` para o line-up. Não há "aprovação" de evento: quem publica é dono; moderação, se vier, é denúncia.
- **`Notification`** (`UserId`, `Type: Simple | Banner | Popup | BandInvite`, `Title`, `Message`, `Link?`, `ImageKey?`, `ReadAt?`, `BandInvitationId?`): o tipo `BandInvite` aponta para a `BandInvitation` por FK, e aceitar/recusar age na invitation, não na notificação.
- **`IssueReport`** (reportar problema): guardado no banco com `Status` e a integração com GitHub Issues vira job enfileirado — a criação do issue não fica no caminho do request.

## Enums herdados do legado (os valores, não o desenho)

- `EMusicGenre`: os 31 gêneros do legado (`Thrash`, `Death`, `Black`, `Doom`, `Heavy`, `Power`, `Progressive`, …). Gravados como coleção numa tabela de ligação (`BandGenre`, `EventGenre`), nunca como `List<Enum>` em coluna — `PrimitiveCollection` é bloqueado pelo `CLAUDE.md` por causa dos dois providers.
- `EAbcCity`: as 9 cidades (`SantoAndre`, `SaoBernardo`, `SaoCaetano`, `Diadema`, `Maua`, `RibeiraoPires`, `RioGrandeDaSerra`, `Outros` …) — a lista fechada é regra de produto: a comunidade é regional.
- `EModerationStatus`, `ENotificationType`, `EBandMemberRole`, `EBandInvitationStatus`, `EEventStatus` como acima.

Enum sai para o cliente como **número** (padrão dos projetos de referência); o front espelha com `const` + `Record` exaustivo.

## Ordem dos módulos, e o que cada um liga no frontend

| # | Módulo | Entidades | Telas mockadas que passam a ser reais |
|---|---|---|---|
| 1 | Perfil e mídia | campos de perfil, upload (avatar/banner) com storage S3-compatível e URL assinada | `/perfil/:nickname` (pessoa), `/minha-conta` |
| 2 | Bandas e convites | `Band`, `BandMember`, `BandInvitation`, `BandGenre` | `/bandas/:slug`, convites em `/notificacoes` |
| 3 | Feed | `Post`, `PostMedia`, `PostLike`, `Comment` | `/feed`, `/feed/:id`, rail de "próximos eventos" só depois do 4 |
| 4 | Eventos | `Event`, `EventBand`, `EventGenre` | `/eventos`, `/eventos/:id`, `/eventos/novo`, `/eventos/:id/editar` |
| 5 | Galeria e moderação | `Photo`, `Video`, permissões `Moderation.*` | `/videos`, `/fotos`, `/admin/moderacao` |
| 6 | Notificações | `Notification` | `/notificacoes`, sino do topbar |
| 7 | Suporte | `IssueReport` + job GitHub | `/reportar-problema` |

A ordem segue dependência: feed e eventos mostram autor e banda, logo perfil e bandas vêm antes. Cada módulo nasce com a sua rule (`perfil.md`, `bandas.md`, …) e a linha dele na tabela do `CLAUDE.md`, e termina apagando o `mocks.ts` da tela correspondente (`frontend/.claude/rules/mocks.md`).

## O que fica fora de propósito

- **Login social**: cada provedor é um fluxo próprio no `AuthAppService`; entra quando houver demanda real, não como "já que estamos aqui".
- **Podcasts** e **loja parceira**: páginas estáticas no front, sem entidade.
- **SignalR para progresso de upload**: o legado usava para uma barra de progresso; o módulo de mídia resolve com upload direto para o storage (URL pré-assinada) e o progresso fica no cliente.
