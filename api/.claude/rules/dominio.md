---
paths:
  - "**/NoiseVault.Domain/Entities/**"
  - "**/NoiseVault.Domain/Enums/**"
---

# Decisões — Domínio da comunidade

O que o Noise Vault é, em entidades, e em que ordem elas nascem. Hoje existem no código `User` (módulo 1) e `Band`, `BandMember`, `BandInvitation` (módulo 2, `bandas.md`); o resto está aqui para o Auth não nascer errado e para cada módulo futuro saber onde encaixa. Decisão de modelagem registrada antes do código é a spec do módulo.

## `User` é sempre uma pessoa; banda não tem login

No legado, uma banda era um `User` disfarçado: registro próprio, e-mail próprio, linha em `BandsUsers` com `IsBand = true` apontando para si mesma. Integrantes ficavam duplicados (JSON em `Band.Integrants` **e** linhas em `BandsUsers`), o instrumento morava no `User` (quem toca em duas bandas só podia ter um), o papel `Band` era calculado na hora do token e convites eram notificações com o id da banda dentro de uma string. Nada disso volta.

- **`User`** (`Name`, `Nickname`, `Email`, `IsActive`) é a conta de uma pessoa. Os dados de perfil (`Bio`, `CityId`, `CityName`, `MainInstrument`, links, `AvatarKey`, `BannerKey`, `BannerPosition`) moram na própria `User`, e os gêneros em `UserGenre` — decidido no módulo 1, ver `perfil-e-midia.md`. `MainInstrument` é o instrumento que a pessoa se apresenta tocando; o instrumento **em cada banda** continua no `BandMember`.
- **`Band`** (`Name`, `Slug`, `Kind: Band | Solo`, `Description`, `City`, `Genres`, `FoundedYear?`, links, `IsVerified`, `AvatarKey?`, `BannerKey?`) é entidade própria, **sem conta**. Quem age em nome da banda é um `BandMember` com `Role = Owner`. URL pública `/bandas/:slug`, ou `/artistas/:slug` para o artista solo, que é uma banda de outro tipo e não uma entidade nova (`bandas.md`, 24/10/2026); `Slug` único e imutável depois de publicado (renomear a banda não quebra link). O ano de fundação é `FoundedYear` (int), não `FoundedAt`: banda sabe o ano, não o dia (`bandas.md`).
- **`BandMember`** (`BandId`, `UserId`, `Instrument`, `Role: Owner | Member`, `JoinedAt`): o instrumento é **da relação**, não da pessoa. `(BandId, UserId)` único. Uma banda tem pelo menos um `Owner`; o último `Owner` não sai sem passar o bastão.
- **`BandInvitation`** (`BandId`, `InvitedUserId`, `InvitedByUserId`, `Instrument`, `Status: Pending | Accepted | Declined | Revoked`, `ExpiresAt`): entidade real, com estado e validade. A notificação do convite é **projeção** desta linha, nunca o armazenamento. Aceitar = transição atômica `Pending → Accepted` no `WHERE` + criação do `BandMember` na mesma transação.
- **Verificação de banda**: `IsVerified` na banda mais `BandVerificationRequest`, o pedido do Owner com estado (pendente, aprovado, recusado), fila de revisão com `Bands.Verifications.View` e `Bands.Verify`, e o nome de banda verificada reservado (`bandas.md`, 15/10/2026). No legado era um formulário 1:1 colado na banda que só virava e-mail.

## Conteúdo: quem publica é sempre um `User`

`Post`, `Event`, `ArchiveEntry` e os comentários têm `AuthorUserId` (FK `Restrict`). Conteúdo "da banda" é conteúdo de um membro **marcado** com `BandId` opcional — é assim que o perfil da banda lista posts sem a banda ter conta.

- **`Post`** (`Content`, `BandId?`, `PublishedAt?`, `EditedAt?`) com `PostMedia (PostId, MediaUploadId, Order)`. A mídia vive antes do post como `MediaUpload` (tipo, estado, chave, dimensões, duração), porque o vídeo converte em segundo plano e precisa existir com estado antes de ser anexado (`midia-e-video.md`). É a mudança do módulo 3 em relação ao `StorageKey` em `PostMedia` que estava previsto aqui. Likes em **`PostLike (PostId, UserId)`** com índice único — nada de `Like` polimórfico sem FK como no legado. Comentário tem **um** pai: `Comment.PostId` obrigatório. Se evento ou foto ganharem comentários, cada um ganha a sua tabela (`EventComment`), não quatro FKs nulas na mesma.
- ~~**`Photo`** e **`Video`** (YouTube) com fila de moderação~~ **saíram** em 13/10/2026, por decisão do usuário. A galeria existia no legado como atalho para usar o YouTube, e com o feed aceitando foto e vídeo de verdade ela viraria um segundo feed, mais fraco. No lugar entrou o **mural das antigas** (`ArchiveEntry` com mídias, bandas, curtidas, "Eu estava lá" e comentários), em que quem tem `Archive.Create` publica direto e o resto da comunidade envia para a curadoria aprovar (`Archive.Review`, `mural.md`). Se um dia houver denúncia de conteúdo do feed, ela nasce como módulo próprio, não como fila de aprovação prévia.
- **`Event`** (`Title`, `Description?`, `StartsAt`, `EndsAt?`, `Venue`, `CityId?`, `CityName?`, `Genres`, `TicketsUrl?`, `Price?`, `Capacity?`, `BannerKey?`, `BannerPosition`, `Status: Published | Cancelled`) com `EventLineupEntry (EventId, BandId?, Name?, Order)` para o line-up: banda cadastrada **ou** nome livre. O módulo 4 tirou o `Draft` (publica direto) e trocou o `EventBand` previsto aqui, porque o line-up precisa aceitar banda de fora do Noise Vault (`eventos.md`). Ao publicar, nasce um `Post` com `EventId` no feed. Não há "aprovação" de evento: quem publica é dono; moderação, se vier, é denúncia.
- **`Notification`** (`UserId`, `Kind`, `ActorUserId?`, `ActorCount`, alvos `PostId?`/`EventId?`/`ArchiveEntryId?`/`BandId?`/`BandInvitationId?`, `LastActivityAt`, `ReadAt?`): o módulo 6 trocou o `Type`/`Title`/`Message` gravados previstos aqui por **dado estruturado**. A frase sai do i18n pelo `Kind`, e o título do alvo é lido ao vivo (`notificacoes.md`). O convite continua apontando para a `BandInvitation` por FK, e aceitar/recusar age na invitation. Banner e popup em massa ficaram para o painel de administração.
- ~~**`IssueReport`** com integração com GitHub Issues num job~~ **trocado** em 14/10/2026, por decisão do usuário: o relato é um **`SupportTicket`** no banco (tags, páginas, fotos e vídeo, contexto do navegador), triado numa tela de administração e conversado com quem reportou (`SupportTicketMessage`). GitHub fica de fora por enquanto (`suporte.md`).

## Enums herdados do legado (os valores, não o desenho)

- `EMusicGenre` (`Domain/Enums/Profiles/EMusicGenre.cs`): 31 valores, **na ordem e com os números já usados no front** (`frontend/src/pages/perfil/genres.ts`: 0 Heavy, 1 Thrash, 2 Death … 30 Other). A lista do legado tinha outra ordem e vários rocks; o contrato numérico que vale é o do front, porque é ele que já existe. Gravados em tabela de ligação (`UserGenre`, depois `BandGenre`, `EventGenre`), nunca como `List<Enum>` em coluna — `PrimitiveCollection` é bloqueado pelo `CLAUDE.md` por causa dos dois providers. 🔴 Valor novo só no fim; reordenar quebra dado gravado.
- ~~`EAbcCity`~~ (as 7 cidades do ABC, São Paulo e "Outra") **saiu** em 14/10/2026: a cidade é a tabela `City` com os municípios do IBGE, mais texto livre para fora do Brasil (`cidades.md`). O núcleo do projeto continua sendo o ABC.
- `ENotificationKind` (`notificacoes.md`), `EBandMemberRole`, `EBandInvitationStatus`, `EEventStatus` como acima; `ESupportTicketStatus`, `ESupportTicketPriority`, `ESupportTag` e `ESupportArea` em `suporte.md`.

Enum sai para o cliente como **número** (padrão dos projetos de referência); o front espelha com `const` + `Record` exaustivo.

## Ordem dos módulos, e o que cada um liga no frontend

| # | Módulo | Entidades | Telas que liga no frontend |
|---|---|---|---|
| 1 ✅ | Perfil e mídia | campos de perfil na `User`, `UserGenre`, upload de avatar/capa em S3 com URL pública (`armazenamento.md`) | `/perfil/:nickname` (pessoa), `/minha-conta`, busca da topbar |
| 2 ✅ | Bandas e convites | `Band`, `BandMember`, `BandInvitation`, `BandGenre` | `/bandas/:slug`, convites em `/notificacoes` |
| 3 ✅ | Feed | `Post`, `PostMedia`, `MediaUpload`, `PostLike`, `Comment` (`feed.md`, `midia-e-video.md`) | `/feed`, `/feed/:id`, rail de "próximos eventos" só depois do 4 |
| 4 ✅ | Eventos | `Event`, `EventLineupEntry`, `EventGenre`, `EventInterest`, `EventComment`, `Post.EventId` (`eventos.md`) | `/eventos`, `/eventos/:id`, `/eventos/novo`, `/eventos/:id/editar` |
| 5 ✅ | ~~Galeria e moderação~~ → **Mural das antigas** | `ArchiveEntry`, `ArchiveMedia`, `ArchiveBandEntry`, `ArchiveLike`, `ArchiveAttendance`, `ArchiveComment`, `Post.ArchiveEntryId`, permissões `Archive.*` (`mural.md`) | `/mural`, `/mural/:id`, `/mural/novo`, `/mural/:id/editar`; `/videos`, `/fotos` e `/admin/moderacao` saíram |
| 6 ✅ | Notificações | `Notification`, `User.EmailOnBandInvite`, SignalR (`/hubs/notifications`), e-mail do convite (`notificacoes.md`) | `/notificacoes`, sino do topbar, preferência em `/minha-conta` |
| 7 ✅ | Suporte | `SupportTicket`, `SupportTicketTag`, `SupportTicketArea`, `SupportTicketMedia`, `SupportTicketMessage`, `Notification.SupportTicketId`, perfil de mídia `Support`, permissões `Support.*` (`suporte.md`) | `/reportar-problema` (desde 27/10/2026, `/ajuda`); nascem `/suporte`, `/suporte/:id`, `/admin/suporte` e `/admin/suporte/:id` |
| 8 ✅ | Envio da comunidade para o mural (ideia 2 do `IDEIAS.md`) | `ArchiveEntry.Status` (`Published`, `Pending`, `Rejected`), `IsSubmission`, revisão e motivo; permissão `Archive.Review` (`mural.md`) | nascem `/mural/enviar`, `/mural/envios` e `/admin/mural` |
| 9 ✅ | @menções e marcação em fotos (ideia 3 do `IDEIAS.md`) | `PhotoTag` (chave em `MediaUploadId`, FK `Cascade`), `ENotificationKind` 23–27; a menção é resolvida do texto, sem tabela (`marcacoes.md`) | composer com autocomplete de @, texto com links e marcação no visualizador de fotos |
| 10 ✅ | Ensaio: músicos e bandas (item 8a do `IDEIAS.md`) | `MusicianProfile` com `MusicianInstrument`, `MusicianGenre`, `MusicianAvailability` e `MusicianSampleLink`; `BandOpening`, `BandOpeningInterest`, `Post.BandOpeningId`, `Notification.BandOpeningId`, kind 28 (`ensaio.md`) | o subdomínio `ensaio.noisevault.net` com a vitrine, a ficha, as vagas e o interesse; selo no perfil, vagas na banda e o post da vaga no feed |
| 11 ✅ | Seguir banda (item 10 do `IDEIAS.md`, a parte do seguir) | `BandFollower`, `BandProfileDto.FollowerCount`/`FollowedByMe`, `GET /api/posts?following=true`, kind 33 (`bandas.md`, `feed.md`) | botão "Seguir" e contador no hero da banda, aba "Seguindo" no `/feed`, "Bandas que sigo" no próprio perfil |
| 12 ✅ | Notícias (pedido do usuário em 21/10/2026, fora do `IDEIAS.md`) | `NewsArticle`, sem autor: é dado coletado de fora por job, não conteúdo da comunidade (`noticias.md`) | nasce `/noticias`, pública |
| 13 ✅ | Artista solo (ideia de um amigo do projeto, 24/10/2026) | `Band.Kind` (`EBandKind`), sem entidade nova (`bandas.md`) | `/artistas/:slug`, `/artistas/nova`, `/artistas/:slug/editar`; o "Criar banda" do perfil vira "Novo projeto" com a escolha entre banda e artista solo |

A ordem segue dependência: feed e eventos mostram autor e banda, logo perfil e bandas vêm antes. Cada módulo nasce com a sua rule (`perfil.md`, `bandas.md`, …) e a linha dele na tabela do `CLAUDE.md`. Até o módulo 7, cada um terminava apagando o `mocks.ts` da tela correspondente; desde então a tela nasce sobre o endpoint real.

## O que fica fora de propósito

- ~~**Login social**: cada provedor é um fluxo próprio no `AuthAppService`; entra quando houver demanda real, não como "já que estamos aqui".~~ A demanda chegou com o site no ar (17/10/2026): Google e Facebook entram pelo `AuthAppService` (`login-social.md`).
- **Podcasts** e **loja parceira**: páginas estáticas no front, sem entidade.
- **SignalR para progresso de upload**: o legado usava para uma barra de progresso; o módulo 3 resolveu com upload multipart direto para o storage (URL pré-assinada) e o progresso fica no cliente. O estado da conversão o cliente consulta por polling (`GET /api/posts/{id}` a cada 5 s, só no post do próprio autor).
