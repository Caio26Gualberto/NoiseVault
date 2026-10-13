---
paths:
  - "**/NoiseVault.Application/Bands/**"
  - "**/NoiseVault.Domain/Entities/Band*.cs"
  - "**/NoiseVault.Api/Controllers/Band*.cs"
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
- `IsVerified` existe e aparece como selo, mas **não há ação para verificar** neste módulo. A permissão `Bands.Verify` e o endpoint de admin entram com a moderação (módulo 5). Até lá o selo é só dado.
- **Seguir banda e contadores** (seguidores, posts, eventos) **não existem**. Seguir não está no domínio, e posts e eventos chegam nos módulos 3 e 4. O DTO não traz campo vazio "para o futuro".

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
- **E-mail e notificação de convite não existem neste módulo.** A pessoa vê o convite em `/notificacoes` (`GET /api/band-invitations/mine`). O e-mail entra com o módulo de notificações (`email.md`).

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

- **`suggested` e `mine` não são paginados de propósito.** `suggested` é um widget de tamanho fixo (5). `mine` é limitado pela regra de volume (no máximo 10 bandas por pessoa).
- **Perfil público**: `UserProfileDto` ganhou `bands: BandSummaryDto[]`, vindo de `IBandAppService.ListSummariesForUser`.

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

## Imagens

São o mesmo fluxo e os mesmos presets do perfil (`armazenamento.md`), agora em `Application/Common/Media/ImageReplacement.cs`: o helper valida, processa e sobe; o AppService grava; depois o arquivo antigo é apagado. Ele foi extraído do `ProfileAppService` quando a banda virou o segundo caso. Chaves: `bands/{bandId}/avatar/{guid}.webp` e `bands/{bandId}/banner/{guid}.webp`.

## Limites

| Política | Limite | Onde |
|---|---|---|
| `band-create` | 5/h por conta | `POST /api/bands` |
| `band-invite` | 30/h por conta | `POST /api/bands/{slug}/invitations` |
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
