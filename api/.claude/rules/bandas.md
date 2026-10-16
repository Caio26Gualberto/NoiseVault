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
- **Seguir banda e contadores de seguidores e eventos** não existem. Seguir não está no domínio, e eventos chegam no módulo 4. `postCount` (posts publicados da banda) entrou com o módulo 3 (`feed.md`). O DTO não traz campo vazio "para o futuro".

## Verificação: o selo diz que a banda é quem diz ser

Pedido do usuário em 15/10/2026: "é importante para ninguém criar uma banda no nome de outra por maldade". No legado, o pedido (nome do responsável, e-mail, telefone, mensagem) só virava um e-mail para o dono do projeto; qualquer pessoa logada pedia por qualquer banda, não havia fila, decisão, motivo nem aviso de volta, e nada impedia uma segunda "Sepultura". Aqui:

- **Só o Owner pede**, pela página da banda (`POST /api/bands/{slug}/verification { message, contactPhone? }`). Nome e e-mail não entram no pedido: são os da conta. A mensagem é obrigatória ("quem é você na banda e como confirmamos"), e o telefone é opcional, para a equipe chamar no WhatsApp. O telefone é dado pessoal: fica na tabela, vai no e-mail da equipe e na fila, e **nunca no log**.
- **Exigência mínima para entrar na fila** (pedido do usuário): avatar, **pelo menos 2 integrantes ativos** (contando o Owner) e **pelo menos um link social**. Sem isso, `BAND_VERIFICATION_REQUIREMENTS` (400) diz o que falta. A ideia é a fila não encher de perfil vazio, e o revisor ter onde conferir (o Instagram ou o YouTube da banda).
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
- **Prefixo**: menos de 3 caracteres ou slug reservado (`nova`, `editar`, que são rotas do front) ganha o prefixo `banda-`.
- **Colisão**: ganha sufixo `-2`, `-3`… A checagem inclui linhas apagadas: o índice único não tem filtro, porque o slug é identidade pública.
- **Imutável**: renomear a banda não muda o slug, então links compartilhados não quebram. Não existe endpoint de troca de slug.
- **Busca por slug**: a URL é normalizada (trim e minúsculas) antes de procurar.

## Quem pode o quê

| Ação | Quem |
|---|---|
| Ver banda, sugeridas, perfil com bandas | qualquer pessoa logada |
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
  3. nickname inexistente ou inativo → `INVITEE_NOT_FOUND`;
  4. convidar a si mesmo → `CANNOT_INVITE_SELF`;
  5. a pessoa já é integrante → `ALREADY_BAND_MEMBER`;
  6. já existe convite pendente válido → `BAND_INVITATION_ALREADY_PENDING`;
  7. a banda já tem 20 pendentes → `BAND_INVITATION_LIMIT_REACHED`.
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

Todos autenticados, em `Controllers/` (escopo do próprio usuário, sem permissão do catálogo).

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
| `GET /api/band-invitations/mine` (paginado, pendentes válidos) | `BAND_INVITATIONS_FOUND` |
| `POST /api/band-invitations/{id}/accept` / `decline` | `BAND_INVITATION_ACCEPTED` / `BAND_INVITATION_DECLINED` |
| `DELETE /api/band-invitations/{id}` | `BAND_INVITATION_REVOKED` |
| `POST /api/bands/{slug}/verification { message, contactPhone? }` (Owner, `band-verification`) | `BAND_VERIFICATION_REQUESTED` |
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
- Verificação: `INVALID_VERIFICATION_MESSAGE`, `INVALID_VERIFICATION_PHONE`, `BAND_VERIFICATION_REQUIREMENTS` (400); `BAND_ALREADY_VERIFIED`, `BAND_VERIFICATION_ALREADY_PENDING`, `BAND_VERIFICATION_NOT_PENDING`, `BAND_NOT_VERIFIED`, `BAND_NAME_RESERVED`, `BAND_NAME_ALREADY_VERIFIED` (409); `BAND_VERIFICATION_NOT_FOUND` (404); `INVALID_VERIFICATION_REJECTION_REASON`, `INVALID_VERIFICATION_STATUS` (400).

## Imagens

São o mesmo fluxo e os mesmos presets do perfil (`armazenamento.md`), agora em `Application/Common/Media/ImageReplacement.cs`: o helper valida, processa e sobe; o AppService grava; depois o arquivo antigo é apagado. Ele foi extraído do `ProfileAppService` quando a banda virou o segundo caso. Chaves: `bands/{bandId}/avatar/{guid}.webp` e `bands/{bandId}/banner/{guid}.webp`.

## Limites

| Política | Limite | Onde |
|---|---|---|
| `band-create` | 5/h por conta | `POST /api/bands` |
| `band-invite` | 30/h por conta | `POST /api/bands/{slug}/invitations` |
| `band-verification` | 3/dia por conta | `POST /api/bands/{slug}/verification` |
| `profile-upload` | 20/h por conta, **compartilhado** com o perfil | uploads de avatar/capa da banda |

Volume (regra de negócio): no máximo **10 bandas por pessoa** (contando participação, na criação e no aceite) e **20 convites pendentes por banda**.

## Eventos de log (contexto `Bands`)

| Evento | Nível | Dados |
|---|---|---|
| `BAND_CREATED` | Info | `BandId`, `Slug` |
| `BAND_INVITATION_SENT` | Info | `InvitationId`, `BandId`, `InvitedUserId` |
| `BAND_MEMBER_JOINED` | Info | `InvitationId`, `BandId` (aceite) |
| `BAND_INVITATION_DECLINED` / `BAND_INVITATION_REVOKED` | Info | `InvitationId` (+ `BandId`) |
| `BAND_MEMBER_PROMOTED` / `BAND_MEMBER_REMOVED` / `BAND_MEMBER_LEFT` | Info | `BandId`, usuário afetado |
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
