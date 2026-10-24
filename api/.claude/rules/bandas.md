---
paths:
  - "**/NoiseVault.Application/Bands/**"
  - "**/NoiseVault.Domain/Entities/Band*.cs"
  - "**/NoiseVault.Api/Controllers/Band*.cs"
  - "**/NoiseVault.Api/Controllers/Admin/BandsAdminController.cs"
  - "**/Repositories/BandVerifications/**"
  - "**/Repositories/BandInvitations/**"
---

# Decisões — Bandas e convites

Módulo 2 da ordem em `dominio.md`. Banda é entidade própria, **sem login**; quem age em nome dela é um `BandMember` com `Role = Owner`. Convite é entidade com estado; a notificação dele (módulo 6) vai ser só projeção.

## Modelo

- **`Band`**: `Name`, `Slug`, `Description`, `City`, `FoundedYear`, links (Instagram, YouTube, Spotify), `IsVerified`, `AvatarKey`, `BannerKey`, `BannerPosition`. Os gêneros ficam em `BandGenres (BandId, Genre)`, no mesmo desenho de `UserGenres`.
- **`FoundedYear` em vez de `FoundedAt`**: banda sabe o ano em que começou, não o dia. Um `DateTime` obrigaria a inventar 1º de janeiro e a exibir uma data falsa.
- **`BandMember`** (`BandId`, `UserId`, `Instrument`, `Role`, `JoinedAt`), com `(BandId, UserId)` único. O instrumento é **da relação**: quem toca guitarra numa banda e baixo em outra tem duas linhas.
  - Sair ou ser removido **apaga a linha** (`DeleteAsync`), porque nada referencia integrante. Voltar é um convite novo.
- **`BandInvitation`** (`BandId`, `InvitedUserId`, `InvitedByUserId`, `Instrument`, `Status`, `ExpiresAt`, `RespondedAt`).
  - Índice único filtrado `(BandId, InvitedUserId) WHERE Status = Pending`, criado com `DatabaseProvider.ValueIndexFilter`.
  - **Expirado não é status**: é `Pending` com `ExpiresAt` no passado. Listas e transições filtram `ExpiresAt > agora`.
  - Antes de criar um convite novo para o mesmo par, os pendentes já vencidos viram `Revoked` (`RetireExpiredAsync`). Sem isso, o índice único travaria o reconvite.
- `IsVerified` é o selo. ~~Não há ação para verificar neste módulo~~: desde 15/10/2026 existe o pedido de verificação com fila de revisão ("Verificação", abaixo).
- **`BandVerificationRequest`** (`BandId`, `RequestedByUserId`, `Message` 20–1000, `ContactPhone?` até 30, `Status: Pending | Approved | Rejected`, `ReviewedByUserId?`, `ReviewedAt?`, `RejectionReason?` 3–500). Índice único filtrado `(BandId) WHERE Status = Pending`: um pedido em análise por banda. O histórico fica: recusar não apaga, e o pedido seguinte é outra linha.
- ~~**Seguir banda e contadores de seguidores e eventos** não existem.~~ Eventos chegaram no módulo 4 e `postCount` no módulo 3 (`feed.md`). **Seguir banda existe desde 16/10/2026** (decisão do usuário: é a relação natural da cena, fã segue banda sem aprovação, e o primeiro sinal honesto para o feed). O DTO continua sem campo vazio "para o futuro".
- **`BandFollower`** (`BandId`, `UserId`, `CreatedAt`), chave composta, sem `EntityBase`, no molde do `EventInterest`. Índice `(UserId, CreatedAt)` para a lista "bandas que sigo". Não existe tabela genérica de relação: amizade entre pessoas, se vier, nasce como entidade própria, e chat não tem nada a ver com nenhuma das duas.
  - **Integrante não segue a própria banda**: `PUT …/follow` de quem é Owner ou Member devolve `BAND_MEMBER_CANNOT_FOLLOW`, e aceitar um convite apaga a linha de seguidor que a pessoa tivesse (`AddMember`). Integrante já vê tudo da banda; contar duas vezes seria contador falso.
  - Seguir e deixar de seguir são **idempotentes** (`PUT`/`DELETE`, como curtida e "Vou") e devolvem o `BandProfileDto` novo. Ficam no limite global de requisições, como curtida: barato e idempotente.
  - `BandProfileDto.FollowerCount` conta seguidores ativos por query, como `PostCount`; `FollowedByMe` diz se quem lê segue. Nada materializado.
  - Quem segue recebe a notificação `FollowedBandPosted` (kind 33) quando a banda publica, inclusive os posts automáticos de evento e de vaga, e o post com vídeo só quando o vídeo fica pronto (`feed.md`, `notificacoes.md`).

## Artista solo: uma banda de outro tipo, não uma entidade nova

Ideia de um amigo do projeto, decidida pelo usuário em 24/10/2026: quem toca sozinho era obrigado a criar uma "banda" que não faz sentido. O artista solo é uma `Band` com **`Kind = Solo`** (`EBandKind`: `Band = 0`, `Solo = 1`), e não uma entidade `Artist` entre `User` e `Band`.
- **Por que não uma entidade `Artist`**: tudo o que a página da banda já tem (discografia, line-up, seguir, verificação, mural, posts marcados, prévia de link, SEO) passaria a existir em dobro, ou `BandMember`, convites, posts, eventos, notificações e Ensaio teriam de apontar para os dois. Um campo resolve o mesmo.
- **Uma pessoa pode ser solo e tocar em bandas ao mesmo tempo**: são só linhas de `BandMember`, e o artista solo conta no limite de 10 bandas por pessoa.
- **URL própria**: o front mostra o solo em `/artistas/:slug`. O slug continua num espaço só, compartilhado com as bandas: `/bandas/x` e `/artistas/x` são a mesma página, e o front corrige o prefixo errado. `BandRules.Section`/`PagePath` é o único lugar da API que monta esse caminho (SEO, prévia, e-mail).
- **Sem integrantes**: o solo tem só quem criou (Owner). Convidar integrante → `SOLO_ARTIST_NO_MEMBERS` (409), e abrir vaga no Ensaio também, porque o "Tenho interesse" vira convite de integrante (`ensaio.md`). Decisão do usuário: nada de "músicos de apoio". No lugar entra a **parceria** "Costuma tocar com" entre páginas (seção abaixo).
- **Trocar o tipo** é campo do `PUT /api/bands/{slug}` (`kind`; ausente mantém o atual):
  - solo → banda: livre;
  - banda → solo com mais de um integrante → `BAND_HAS_MEMBERS` (409), e o dono remove os outros antes;
  - banda → solo com vaga aberta no Ensaio → `BAND_HAS_OPEN_OPENINGS` (409);
  - na troca para solo, os convites de integrante pendentes viram `Revoked` (`RevokePendingForBandAsync`), em vez de travar a troca.
  - O slug não muda (é identidade pública), mesmo que tenha nascido com o prefixo `banda-`.
- **Verificação**: o solo pede com **1** integrante, o próprio dono (`BandVerificationRules.MinMembers`). Avatar e link social continuam obrigatórios. O nome verificado reservado vale entre os dois tipos: um artista solo verificado reserva o nome contra banda nova, e vice-versa.
- `kind` sai em `BandSummaryDto`, `BandProfileDto`, na banda da notificação e em `MentionDto.bandKind`, para o front montar o link certo sem outra ida. A busca aceita `kind` opcional.

## Parceria: "Costuma tocar com", só com o aceite do outro lado

Pedido do usuário em 24/10/2026: artista solo costuma tocar com outros artistas solo, e "músico de apoio" não descreve isso. A **`BandPartnership`** liga duas **páginas** (banda ou artista solo, nos dois lados e em qualquer combinação), não pessoas. 🔴 Ela só aparece depois que o outro lado aceita: "a pessoa pode não querer se associar do lado dela" (o usuário).

- **Modelo**: `RequesterBandId`, `TargetBandId`, `LowBandId`/`HighBandId` (o par em ordem, para a unicidade valer nos dois sentidos), `InvitedByUserId`, `RespondedByUserId?`, `Status: Pending | Accepted | Declined | Revoked`, `ExpiresAt`, `RespondedAt?`. Índice único filtrado em `(LowBandId, HighBandId) WHERE Status IN (Pending, Accepted)` (`DatabaseProvider.ValuesIndexFilter`): no máximo uma parceria viva por par.
- **Convite**: um Owner da página A convida a página B pelo slug. Todos os Owners de B recebem a notificação 34. Vale **14 dias**, e "expirado" é `Pending` com `ExpiresAt` no passado, como no convite de integrante (os vencidos do par viram `Revoked` antes de um convite novo).
- **Enquanto está pendente, não aparece em lugar nenhum público**, nem na página de quem convidou. A lista pública (`GET …/partnerships`) só traz `Accepted`.
- **Aceitar** (qualquer Owner de B) é a transição atômica `Pending → Accepted` no `WHERE` (`TryRespondAsync`, com o Owner checado na própria query). A parceria passa a aparecer nas duas páginas, e os Owners de A recebem a notificação 35.
- **Recusar é silencioso**: ninguém é notificado. Quem convidou vê só "não aceito" na lista de pendentes da própria página. Depois de uma recusa, **A não convida B de novo por 30 dias** (`BAND_PARTNERSHIP_COOLDOWN`). B continua podendo convidar A.
- **Desfazer** (`DELETE /api/band-partnerships/{id}`, transição para `Revoked`):
  - pendente: só o lado que convidou cancela;
  - aceita: qualquer Owner dos dois lados desfaz, sem pedir licença, e a parceria some das duas páginas.
  - Não há notificação ao desfazer. A notificação do convite revogado some pelo filtro de visibilidade, como a do convite de integrante.
- **Limites**: 20 convites pendentes enviados por página (`BAND_PARTNERSHIP_LIMIT_REACHED`) e 50 parcerias aceitas por página (`BAND_PARTNER_LIMIT_REACHED`, conferido no convite e no aceite, para os dois lados). Rate limit `band-partnership` com 30/h por conta.
- **No SEO**, a página ganha a seção "Costuma tocar com", com link para as páginas parceiras (`seo.md`).

## Verificação: o selo diz que a banda é quem diz ser

Pedido do usuário em 15/10/2026: "é importante para ninguém criar uma banda no nome de outra por maldade". No legado, o pedido (nome do responsável, e-mail, telefone, mensagem) só virava um e-mail para o dono do projeto; qualquer pessoa logada pedia por qualquer banda, não havia fila, decisão, motivo nem aviso de volta, e nada impedia uma segunda "Sepultura". Aqui:

- **Só o Owner pede**, pela página da banda (`POST /api/bands/{slug}/verification { message, contactPhone? }`). Nome e e-mail não entram no pedido: são os da conta. A mensagem é obrigatória ("quem é você na banda e como confirmamos"), e o telefone é opcional, para a equipe chamar no WhatsApp. O telefone é dado pessoal: fica na tabela, vai no e-mail da equipe e na fila, e **nunca no log**.
- **Exigência mínima para entrar na fila** (pedido do usuário): avatar, **pelo menos 2 integrantes ativos** (contando o Owner; 1 no artista solo) e **pelo menos um link social**. Sem isso, `BAND_VERIFICATION_REQUIREMENTS` (400) diz o que falta. A ideia é a fila não encher de perfil vazio, e o revisor ter onde conferir (o Instagram ou o YouTube da banda).
- Banda já verificada → `BAND_ALREADY_VERIFIED` (409); pedido pendente → `BAND_VERIFICATION_ALREADY_PENDING` (409). Depois de recusa a banda pede de novo (linha nova). Limite `band-verification` 3/dia por conta.
- **Fila de revisão** em `/api/admin/bands/verifications`, no molde dos envios do mural (`mural.md`): lista paginada por status com busca (nome ou slug da banda, nome ou apelido de quem pediu), resumo por status, aprovar e recusar com motivo. Pendentes vêm do mais antigo para o mais novo. **Aprovar e recusar são atômicos** (`TryReviewAsync`, `WHERE Status = Pending`; zero linhas → `BAND_VERIFICATION_NOT_PENDING`). Aprovar liga `IsVerified` na mesma transação.
- **Tirar o selo** (`POST /api/admin/bands/{id}/unverify`): para quando o perfil troca de mão ou a parceria quebra. `BAND_NOT_VERIFIED` (409) se não tinha selo. Sem motivo: é ação de equipe, auditada pela trilha global (`auditoria.md`).
- **Permissões** (grupo "Bandas" no catálogo): `Bands.Verifications.View` (ver a fila, a mensagem e o contato) e `Bands.Verify` (aprovar, recusar, tirar o selo; pai `View`). Nenhum papel padrão as recebe; `Admin` tem implícito. Um papel "Curador de bandas" criado pelo painel passa a revisar sem mudar código.
- **O que de fato fecha a porta para a maldade é o nome.** Criar ou renomear uma banda com o mesmo nome (sem diferenciar maiúsculas) de uma banda **verificada** é recusado com `BAND_NAME_RESERVED` (409). Na aprovação, se já houver outra verificada com o mesmo nome, `BAND_NAME_ALREADY_VERIFIED` (409): a equipe decide qual é a de verdade antes. Banda não verificada continua podendo ter homônima (slug `-2`), porque banda pequena com nome repetido existe de verdade.
- **Avisos**: quem tem `Bands.Verifications.View` recebe a notificação `BandVerificationRequested` (menos quem pediu) **e um e-mail** na caixa de `Email:VerificationInbox` (pedido do usuário: "novos pedidos chegam no meu e-mail"), enviado por job como o convite de banda (`notificacoes.md`, `email.md`). Os Owners recebem `BandVerificationApproved`, `BandVerificationRejected` (o motivo aparece na página da banda, em `verification.rejectionReason`) ou `BandVerificationRevoked`.
- **No `BandProfileDto`**, `verification` (`status`, `requestedAt`, `reviewedAt`, `rejectionReason`) é o último pedido, e só vai para **Owner**; para os outros é nulo. É o que a página usa para mostrar "pedir", "em análise" ou "recusado: motivo".
- **Denúncia de perfil falso** não ganhou tela própria: o "Reportar problema" com a área Bandas (`suporte.md`) já serve.

## Slug: gerado uma vez, nunca muda

- **Geração**: o slug sai do nome na criação (`BandRules.BaseSlug`). O nome vai para minúsculas e perde os acentos; cada sequência que não for `a-z0-9` vira `-`; o resultado fica com 3 a 60 caracteres.
- **Prefixo**: menos de 3 caracteres ou slug reservado (`nova`, `editar`, que são rotas do front) ganha o prefixo `banda-`, ou `artista-` no artista solo.
- **Colisão**: ganha sufixo `-2`, `-3`… A checagem inclui linhas apagadas: o índice único não tem filtro, porque o slug é identidade pública.
- **Imutável**: renomear a banda não muda o slug, então links compartilhados não quebram. Não existe endpoint de troca de slug.
- **Busca por slug**: a URL é normalizada (trim e minúsculas) antes de procurar.

## Quem pode o quê

| Ação | Quem |
|---|---|
| Ver banda, discografia, shows e registros da banda | **qualquer pessoa, sem login** (só leitura, desde 20/10/2026, `seo.md`) |
| Sugeridas, perfil com bandas, busca de bandas | qualquer pessoa logada |
| Criar banda | qualquer pessoa logada; quem cria vira Owner na mesma transação |
| Editar dados, avatar, capa | Owner |
| Convidar, listar convites pendentes da banda, revogar | Owner |
| Promover integrante a Owner | Owner |
| Remover integrante | Owner remove **Member**; Owner não remove outro Owner (`BAND_OWNER_NOT_REMOVABLE`) |
| Sair | qualquer integrante, exceto o **último Owner** (`BAND_LAST_OWNER`) |
| Aceitar/recusar convite | só a pessoa convidada |

- **Pode haver mais de um Owner.** Passar a posse é promover alguém e depois sair; não existe uma operação "transferir".
- **Checagem de Owner fica no `WHERE`**: `Slug == x AND Members.Any(m => m.UserId == eu AND m.Role == Owner)`. Quando volta nulo, uma segunda consulta separa 404 `BAND_NOT_FOUND` de 403 `BAND_FORBIDDEN`. O formulário de edição precisa saber a diferença.
- **Corrida aceita no `Leave`**: dois Owners saindo no mesmo instante podem deixar a banda sem Owner, porque a contagem e o delete são duas idas ao banco. É raro e reversível pelo banco. Fechar isso exigiria `SERIALIZABLE` ou lock de linha em todo o módulo; não compensa agora.

## Convite

- `POST /api/bands/{slug}/invitations { nickname, instrument }` recusa nesta ordem:
  1. instrumento com mais de 60 caracteres → `INVALID_INSTRUMENT`;
  2. quem chama não é Owner → `BAND_FORBIDDEN`;
  3. a página é de artista solo → `SOLO_ARTIST_NO_MEMBERS`;
  4. nickname inexistente ou inativo → `INVITEE_NOT_FOUND`;
  5. convidar a si mesmo → `CANNOT_INVITE_SELF`;
  6. a pessoa já é integrante → `ALREADY_BAND_MEMBER`;
  7. já existe convite pendente válido → `BAND_INVITATION_ALREADY_PENDING`;
  8. a banda já tem 20 pendentes → `BAND_INVITATION_LIMIT_REACHED`.
- Validade de **14 dias** (`BandRules.InvitationLifetime`).
- **Aceitar** é transição condicional em `IBandInvitationRepository.TryRespondAsync`:
  - O `ExecuteUpdateAsync` tem `WHERE Id = x AND InvitedUserId = eu AND Status = Pending AND ExpiresAt > agora`.
  - Na **mesma transação** do `IUnitOfWork`, o `BandMember` é criado por `IBandAppService.AddMember`, com o instrumento do convite.
  - Zero linhas afetadas → `BAND_INVITATION_NOT_PENDING`. Dois cliques em "aceitar" criam um integrante só.
  - Antes da transação há duas guardas de mensagem: `ALREADY_BAND_MEMBER` e `BAND_LIMIT_REACHED`.
- **Recusar** usa a mesma transição, para `Declined`. **Revogar** (`DELETE /api/band-invitations/{id}`) é `Pending → Revoked`, só para Owner da banda do convite.
- `ExecuteUpdateAsync` não passa pelo `ChangeTracker`, então a transição grava `UpdatedAt`/`UpdatedBy` por conta própria. A trilha da transição é o evento de log (abaixo).
- ~~**E-mail e notificação de convite não existem neste módulo.**~~ O módulo 6 acrescentou os dois: o convite gera a notificação `BandInvite` e um e-mail por job; aceitar, recusar, promover, remover e sair também notificam (`notificacoes.md`). A pessoa continua respondendo nos convites do painel do sino (antes, a página `/notificacoes`) (`GET /api/band-invitations/mine`).

## Endpoints

Em `Controllers/` (escopo do próprio usuário, sem permissão do catálogo). Todos pedem login, menos os `GET` de leitura da banda listados em `seo.md`.

| Método e rota | Code de sucesso |
|---|---|
| `POST /api/bands` (`CreateBandInputDto`: dados + `instrument` de quem cria) | `BAND_CREATED` |
| `GET /api/bands/suggested` (5 mais recentes de que você não faz parte) | `BANDS_FOUND` |
| `GET /api/bands/mine` | `BANDS_FOUND` |
| `GET /api/bands/{slug}` → `BandProfileDto` com `members` e `myRole` | `BAND_FOUND` |
| `PUT /api/bands/{slug}` (`BandInputDto`, substitui tudo como o perfil) | `BAND_UPDATED` |
| `POST`/`DELETE /api/bands/{slug}/avatar`, `POST`/`DELETE …/banner`, `PATCH …/banner-position` | os mesmos codes de imagem do perfil (`AVATAR_UPDATED`…) |
| `GET /api/bands/{slug}/invitations` (paginado, pendentes válidos) | `BAND_INVITATIONS_FOUND` |
| `POST /api/bands/{slug}/invitations` | `BAND_INVITATION_SENT` |
| `DELETE /api/bands/{slug}/members/{userId}` | `BAND_MEMBER_REMOVED` |
| `POST /api/bands/{slug}/members/{userId}/promote` (idempotente) | `BAND_MEMBER_PROMOTED` |
| `POST /api/bands/{slug}/leave` | `BAND_LEFT` |
| `PUT /api/bands/{slug}/follow` · `DELETE …/follow` (idempotentes, devolvem o perfil) | `BAND_FOLLOWED` / `BAND_UNFOLLOWED` |
| `GET /api/bands/following` (paginado, do seguido mais recente para o mais antigo) | `BANDS_FOUND` |
| `GET /api/band-invitations/mine` (paginado, pendentes válidos) | `BAND_INVITATIONS_FOUND` |
| `POST /api/band-invitations/{id}/accept` / `decline` | `BAND_INVITATION_ACCEPTED` / `BAND_INVITATION_DECLINED` |
| `DELETE /api/band-invitations/{id}` | `BAND_INVITATION_REVOKED` |
| `POST /api/bands/{slug}/verification { message, contactPhone? }` (Owner, `band-verification`) | `BAND_VERIFICATION_REQUESTED` |
| `GET /api/bands/{slug}/partnerships` (sem login, paginado, só aceitas, da mais recente) | `BAND_PARTNERSHIPS_FOUND` |
| `GET /api/bands/{slug}/partnerships/pending` (Owner: pendentes enviadas e recebidas, e as recusadas dentro da carência) | `BAND_PARTNERSHIPS_FOUND` |
| `POST /api/bands/{slug}/partnerships { targetSlug }` (Owner, `band-partnership`) | `BAND_PARTNERSHIP_SENT` |
| `POST /api/band-partnerships/{id}/accept` · `…/decline` (Owner da página convidada) | `BAND_PARTNERSHIP_ACCEPTED` / `BAND_PARTNERSHIP_DECLINED` |
| `DELETE /api/band-partnerships/{id}` | `BAND_PARTNERSHIP_REMOVED` |
| `GET /api/admin/bands/verifications?status=&search=` (`Bands.Verifications.View`) | `BAND_VERIFICATIONS_FOUND` |
| `GET /api/admin/bands/verifications/summary` → `{ pending, approved, rejected }` | `BAND_VERIFICATION_SUMMARY_FOUND` |
| `POST /api/admin/bands/verifications/{id}/approve` · `…/reject { reason }` (`Bands.Verify`) | `BAND_VERIFICATION_APPROVED` / `BAND_VERIFICATION_REJECTED` |
| `POST /api/admin/bands/{id}/unverify` (`Bands.Verify`) | `BAND_VERIFICATION_REVOKED` |

- **Busca de bandas** (`GET /api/bands/search?search=`, paginada, 2+ caracteres, nome ou slug, rate limit `user-search`) entrou com o módulo 4 para o line-up de eventos.
- **`upcomingEventCount`** no `BandProfileDto` conta os eventos publicados que ainda não rolaram, organizados pela banda ou com ela no line-up (`eventos.md`).
- **`suggested` e `mine` não são paginados de propósito.** `suggested` é um widget de tamanho fixo (5). `mine` é limitado pela regra de volume (no máximo 10 bandas por pessoa).
- **Perfil público**: `UserProfileDto` ganhou `bands: BandSummaryDto[]`, vindo de `IBandAppService.ListSummariesForUser`.
- **Vagas abertas** (`BandOpening`) e o "Tenho interesse" que vira convite moram em `ensaio.md`. `GET /api/bands/owned` lista as bandas de que a pessoa é Owner, para o front escolher em nome de qual abrir vaga ou convidar.
- **Discografia** (`BandRelease`, `BandTrack`, importação do Spotify, faixa em destaque) mora em `discografia.md`; as rotas ficam em `/api/bands/{slug}/discography`.

**Validação** (as mesmas regras do perfil quando o campo é o mesmo):

| Campo | Regra | Code |
|---|---|---|
| `kind` | `0` banda ou `1` artista solo; ausente é banda na criação e o tipo atual na edição | `INVALID_BAND_KIND` |
| `name` | 2–100 depois de trim e espaços colapsados | `INVALID_BAND_NAME` |
| `description` | até 1000 | `INVALID_BAND_DESCRIPTION` |
| `city`, `genres`, `socialLinks.*` | `ProfileRules` | `INVALID_CITY`, `INVALID_GENRE`, `INVALID_SOCIAL_LINK` |
| `foundedYear` | 1950 até o ano atual (`AppTime`), ou nulo | `INVALID_FOUNDED_YEAR` |
| `instrument` | até 60 | `INVALID_INSTRUMENT` |

**Outros erros:**
- `BAND_NOT_FOUND` (404);
- `BAND_FORBIDDEN` (403);
- `BAND_LIMIT_REACHED` (409, mais de 10 bandas);
- `BAND_MEMBER_NOT_FOUND` (404);
- `BAND_INVITATION_NOT_FOUND` (404, inclusive quando o convite é de outra pessoa).
- Artista solo: `SOLO_ARTIST_NO_MEMBERS`, `BAND_HAS_MEMBERS`, `BAND_HAS_OPEN_OPENINGS` (409).
- Parceria, nesta ordem no convite: `BAND_NOT_FOUND`/`BAND_FORBIDDEN` da página de quem convida; `BAND_PARTNER_NOT_FOUND` (404); `CANNOT_PARTNER_SELF` (400); `ALREADY_PARTNERS`, `BAND_PARTNERSHIP_ALREADY_PENDING` (pendente em qualquer sentido), `BAND_PARTNERSHIP_COOLDOWN`, `BAND_PARTNERSHIP_LIMIT_REACHED`, `BAND_PARTNER_LIMIT_REACHED` (409). Na resposta: `BAND_PARTNERSHIP_NOT_FOUND` (404, também para quem não é Owner da página convidada), `BAND_PARTNERSHIP_NOT_PENDING` e `BAND_PARTNERSHIP_NOT_REMOVABLE` (409, pendente cancelado pelo lado convidado).
- Verificação: `INVALID_VERIFICATION_MESSAGE`, `INVALID_VERIFICATION_PHONE`, `BAND_VERIFICATION_REQUIREMENTS` (400); `BAND_ALREADY_VERIFIED`, `BAND_VERIFICATION_ALREADY_PENDING`, `BAND_VERIFICATION_NOT_PENDING`, `BAND_NOT_VERIFIED`, `BAND_NAME_RESERVED`, `BAND_NAME_ALREADY_VERIFIED` (409); `BAND_VERIFICATION_NOT_FOUND` (404); `INVALID_VERIFICATION_REJECTION_REASON`, `INVALID_VERIFICATION_STATUS` (400).

## Imagens

São o mesmo fluxo e os mesmos presets do perfil (`armazenamento.md`), agora em `Application/Common/Media/ImageReplacement.cs`: o helper valida, processa e sobe; o AppService grava; depois o arquivo antigo é apagado. Ele foi extraído do `ProfileAppService` quando a banda virou o segundo caso. Chaves: `bands/{bandId}/avatar/{guid}.webp` e `bands/{bandId}/banner/{guid}.webp`.

## Limites

| Política | Limite | Onde |
|---|---|---|
| `band-create` | 5/h por conta | `POST /api/bands` |
| `band-invite` | 30/h por conta | `POST /api/bands/{slug}/invitations` |
| `band-partnership` | 30/h por conta | `POST /api/bands/{slug}/partnerships` |
| `band-verification` | 3/dia por conta | `POST /api/bands/{slug}/verification` |
| `profile-upload` | 20/h por conta, **compartilhado** com o perfil | uploads de avatar/capa da banda |

Volume (regra de negócio): no máximo **10 bandas por pessoa** (contando participação, na criação e no aceite), **20 convites pendentes por banda**, **20 convites de parceria pendentes** e **50 parcerias aceitas** por página.

## Eventos de log (contexto `Bands`)

| Evento | Nível | Dados |
|---|---|---|
| `BAND_CREATED` | Info | `BandId`, `Slug` |
| `BAND_KIND_CHANGED` | Info | `BandId`, `From`, `To` |
| `BAND_PARTNERSHIP_SENT` | Info | `PartnershipId`, `RequesterBandId`, `TargetBandId` |
| `BAND_PARTNERSHIP_ACCEPTED` / `BAND_PARTNERSHIP_DECLINED` / `BAND_PARTNERSHIP_REMOVED` | Info | `PartnershipId` (+ as bandas no aceite, o status anterior no remover) |
| `BAND_INVITATION_SENT` | Info | `InvitationId`, `BandId`, `InvitedUserId` |
| `BAND_MEMBER_JOINED` | Info | `InvitationId`, `BandId` (aceite) |
| `BAND_INVITATION_DECLINED` / `BAND_INVITATION_REVOKED` | Info | `InvitationId` (+ `BandId`) |
| `BAND_MEMBER_PROMOTED` / `BAND_MEMBER_REMOVED` / `BAND_MEMBER_LEFT` | Info | `BandId`, usuário afetado |
| `BAND_FOLLOWED` / `BAND_UNFOLLOWED` | Info | `BandId` (só quando a linha muda de fato) |
| `AVATAR_*` / `BANNER_*` | Info | `BandId`, `Key` |
| `BAND_VERIFICATION_REQUESTED` / `BAND_VERIFICATION_APPROVED` / `BAND_VERIFICATION_REJECTED` | Info | `RequestId`, `BandId` (mensagem, telefone e motivo não entram) |
| `BAND_VERIFICATION_REVOKED` | Info | `BandId` |
| `BAND_VERIFICATION_EMAIL_SENT` / `BAND_VERIFICATION_EMAIL_FAILED` / `BAND_VERIFICATION_EMAIL_ENQUEUE_FAILED` | Info / Warning / Error | `RequestId` |

Editar o texto da banda não gera log, pelo mesmo motivo do perfil. A auditoria global só registra ator da equipe (`auditoria.md`), então, para o usuário comum, estes eventos **são** a trilha do módulo.

## Roteiro de verificação

Com `docker compose up -d` e a API no ar: conta A (admin) e conta B registrada e confirmada pelo Mailpit. Um script com esses passos vive fora do repositório (`bands-api.mjs` no scratchpad da sessão que criou o módulo).

1. **Criar.** A cria "Fúria de Aço" com `instrument: "Vocal"` → `BAND_CREATED`, `slug: furia-de-aco`, `myRole: 0`, 1 integrante. O mesmo nome de novo → `furia-de-aco-2`. Nome `x` → `INVALID_BAND_NAME`; ano 2999 → `INVALID_FOUNDED_YEAR`.
2. **Ver e editar.** `GET` por B → `BAND_FOUND`; slug inexistente → 404 `BAND_NOT_FOUND`. `PUT` por B → 403 `BAND_FORBIDDEN`; `PUT` por A → `BAND_UPDATED`.
3. **Convidar.**
   - A convida a si mesmo → `CANNOT_INVITE_SELF`.
   - Nickname inexistente → `INVITEE_NOT_FOUND`.
   - B tenta convidar → `BAND_FORBIDDEN`.
   - A convida B (Guitarra) → `BAND_INVITATION_SENT`; de novo → `BAND_INVITATION_ALREADY_PENDING`.
   - `GET …/invitations` (A) e `GET /api/band-invitations/mine` (B) mostram o convite.
4. **Aceitar.**
   - A tenta aceitar o convite de B → 404 `BAND_INVITATION_NOT_FOUND`.
   - B aceita → `BAND_INVITATION_ACCEPTED`; aceitar de novo → `ALREADY_BAND_MEMBER`; recusar → `BAND_INVITATION_NOT_PENDING`.
   - A banda lista B como Member com "Guitarra".
5. **Passar a posse.** A sair → `BAND_LAST_OWNER`; A remover a si (Owner) → `BAND_OWNER_NOT_REMOVABLE`. A promove B → `BAND_MEMBER_PROMOTED`; A sai → `BAND_LEFT`; `PUT` de A → 403.
6. **Recusar e revogar.** B convida A e A recusa → `BAND_INVITATION_DECLINED`. Novo convite: A revogar → 403; B revoga → `BAND_INVITATION_REVOKED`; A aceitar → `BAND_INVITATION_NOT_PENDING`.
7. **Imagens.** B sobe avatar → `AVATAR_UPDATED`, URL `bands/{id}/avatar/…webp` responde 200 no LocalStack. Capa → `BANNER_UPDATED`. A sobe avatar → 403.
8. **Listas.** `GET /api/users/{B}` → `bands` com a banda; `GET /api/bands/suggested` para A não traz banda de que A faz parte; `GET /api/bands/mine` (B) → `BANDS_FOUND`.
9. **Log.** As linhas `(bands) BAND_CREATED`, `BAND_INVITATION_SENT`, `BAND_MEMBER_JOINED`, `BAND_MEMBER_PROMOTED`, `BAND_MEMBER_LEFT`, `BAND_INVITATION_DECLINED`, `BAND_INVITATION_REVOKED` saem com `user=` e `op=`.

### Verificação

Conta A (admin), conta B (Owner de uma banda) e conta C (integrante). Mailpit no ar.

1. **Exigência.** B pede a verificação de uma banda sem avatar e só com B → `BAND_VERIFICATION_REQUIREMENTS` listando `avatar`, `at least 2 members`, `a social link`. B sobe o avatar, convida C (que aceita) e preenche o Instagram → o pedido passa com `BAND_VERIFICATION_REQUESTED`; mensagem com 10 caracteres → `INVALID_VERIFICATION_MESSAGE`; C pedindo → `BAND_FORBIDDEN`.
2. **Avisos.** A recebe a notificação 29 e o Mailpit mostra "Novo pedido de verificação de banda" na caixa de `Email:VerificationInbox`, com a mensagem, o telefone e os dois links. O `GET /api/bands/{slug}` de B traz `verification.status = 0`; o de C traz `verification: null`.
3. **Fila.** C em `GET /api/admin/bands/verifications` → 403. A vê o pedido em `status=0`, o resumo com `pending: 1`, e a busca pelo apelido de B acha. Pedir de novo → `BAND_VERIFICATION_ALREADY_PENDING`.
4. **Recusa e novo pedido.** A recusa sem motivo → `INVALID_VERIFICATION_REJECTION_REASON`; com motivo → `BAND_VERIFICATION_REJECTED`, B recebe a 31 e vê o motivo em `verification.rejectionReason`. B pede de novo → nova linha pendente; aprovar a antiga → `BAND_VERIFICATION_NOT_PENDING`.
5. **Aprovação e nome.** A aprova → `BAND_VERIFICATION_APPROVED`, `isVerified: true`, B e os outros Owners recebem a 30. Criar outra banda com o mesmo nome (em caixa diferente) → `BAND_NAME_RESERVED`; renomear uma banda para esse nome → `BAND_NAME_RESERVED`. Pedir de novo → `BAND_ALREADY_VERIFIED`.
6. **Selo removido.** `POST /api/admin/bands/{id}/unverify` → `BAND_VERIFICATION_REVOKED`, `isVerified: false`, os Owners recebem a 32; de novo → `BAND_NOT_VERIFIED`. O 4º pedido no mesmo dia → 429.

### Seguir

1. B segue `furia-de-aco` (`PUT /api/bands/furia-de-aco/follow`) → `BAND_FOLLOWED`, `followedByMe: true`, `followerCount: 1`; de novo → o mesmo, sem segunda linha. A (Owner) tenta seguir → `BAND_MEMBER_CANNOT_FOLLOW`.
2. `GET /api/bands/following` de B lista a banda; `GET /api/posts?following=true` de B traz só posts com `BandId` das bandas que B segue.
3. A publica como a banda → B recebe a notificação 33 com `post` e `band`; A não recebe. Com vídeo, a 33 só chega quando o Hangfire publica o post.
4. A convida B e B aceita → B deixa de ser seguidor (`followerCount: 0`) e passa a integrante.
5. `DELETE …/follow` → `BAND_UNFOLLOWED`; de novo → o mesmo code, nada apagado.

### Artista solo

1. A cria `{ kind: 1, name: "Okamoto", instrument: "Guitarra" }` → `BAND_CREATED`, `kind: 1`; `kind: 7` → `INVALID_BAND_KIND`. Um solo chamado "Zé" → slug `artista-ze`.
2. A convida B no solo → `SOLO_ARTIST_NO_MEMBERS`; abrir vaga no Ensaio → `SOLO_ARTIST_NO_MEMBERS`.
3. Banda com 2 integrantes e `PUT { kind: 1, … }` → `BAND_HAS_MEMBERS`; com vaga aberta → `BAND_HAS_OPEN_OPENINGS`; só com o Owner e um convite pendente → `BAND_UPDATED`, `kind: 1`, o convite vira `Revoked` e o log `BAND_KIND_CHANGED` sai. `PUT` sem `kind` mantém o tipo.
4. Solo com avatar e Instagram pede verificação com 1 integrante → `BAND_VERIFICATION_REQUESTED`; o e-mail da equipe diz "do artista solo" e aponta para `/artistas/{slug}`.
5. `GET /api/bands/search?search=oka&kind=1` só traz solo; `GET /seo/artistas/okamoto` e `/seo/bandas/okamoto` → mesmo HTML, `canonical` em `/artistas/okamoto`, lead "Okamoto é artista solo de…" e sem a seção de integrantes; `/s/artistas/okamoto` → "Artista solo · Ribeirão Pires"; o sitemap lista `/artistas/okamoto`.

### Parceria

Conta A (Owner do solo `okamoto`), conta B (Owner do solo `bia-santana`) e conta C (Owner de uma banda).

1. A convida `bia-santana` → `BAND_PARTNERSHIP_SENT`, B recebe a notificação 34 com `partnership.myBand` = `bia-santana`. `GET /api/bands/okamoto/partnerships` (sem login) → vazio; `…/pending` de A → a parceria com `direction: 0`; o mesmo de B → `direction: 1`. A convida de novo → `BAND_PARTNERSHIP_ALREADY_PENDING`; B convidando A → o mesmo; A convidando `okamoto` → `CANNOT_PARTNER_SELF`; slug inexistente → `BAND_PARTNER_NOT_FOUND`; C chamando o convite em nome de `okamoto` → `BAND_FORBIDDEN`.
2. C tenta aceitar → `BAND_PARTNERSHIP_NOT_FOUND`; A tenta aceitar o próprio convite → o mesmo. B aceita → `BAND_PARTNERSHIP_ACCEPTED`; A recebe a 35; a notificação 34 de B fica lida. Aceitar de novo → `BAND_PARTNERSHIP_NOT_PENDING`. As duas listas públicas trazem a outra página; `/seo/artistas/okamoto` mostra "Costuma tocar com".
3. A convida de novo → `ALREADY_PARTNERS`. B desfaz (`DELETE`) → `BAND_PARTNERSHIP_REMOVED`, some das duas listas.
4. A convida a banda de C; C recusa → `BAND_PARTNERSHIP_DECLINED`, nenhuma notificação para A, e o `…/pending` de A traz a parceria com `status: 2`. A convida de novo → `BAND_PARTNERSHIP_COOLDOWN`; C convidando A → `BAND_PARTNERSHIP_SENT`.
5. Pendente: C (convidado) tenta `DELETE` → `BAND_PARTNERSHIP_NOT_REMOVABLE`; A cancela → `BAND_PARTNERSHIP_REMOVED`, e a notificação some da lista de C.
6. Log: `BAND_PARTNERSHIP_SENT`, `BAND_PARTNERSHIP_ACCEPTED`, `BAND_PARTNERSHIP_DECLINED`, `BAND_PARTNERSHIP_REMOVED` com `user=` e `op=`. O 31º convite na mesma hora → 429.
