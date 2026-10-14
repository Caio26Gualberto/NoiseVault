---
paths:
  - "**/NoiseVault.Application/Events/**"
  - "**/NoiseVault.Infra.Data/Repositories/Events/**"
  - "**/NoiseVault.Api/Controllers/EventsController.cs"
  - "**/NoiseVault.Domain/Entities/Event*.cs"
---

# Decisões — Eventos

Módulo 4 da ordem em `dominio.md`. A agenda serve tanto para o show do porão quanto para a turnê no estádio. O exemplo do usuário foi Iron Maiden no Allianz Parque, com link para a Eventim: qualquer pessoa cadastra o evento que vai acontecer, mesmo que não seja dela.

## Modelo

- **`Event`**:
  - `OrganizerUserId` e `BandId?` (publicado como banda, igual ao post);
  - `Title`, `Description?`, `StartsAt`, `EndsAt?`, `EndsAtOrDefault`;
  - `Venue`, `City` (`EAbcCity`), `CityName?`;
  - `TicketsUrl?`, `Price?`, `Capacity?`;
  - `BannerKey?`, `BannerPosition`, `Status` (`Published` 0, `Cancelled` 1), `CancelledAt?`.
- **Sem rascunho** (decisão do usuário): publica direto. O `Draft` que `dominio.md` previa saiu.
- **`CityName`** só existe com `City = Other`. É o show fora do ABC e de SP ("Curitiba"). A lista fechada de cidades continua valendo para o resto.
- **`Price`** é "a partir de": show grande tem vários setores, então vale o menor preço. Nulo quer dizer entrada franca.
- **`EndsAtOrDefault`** é gravado pelo AppService: `EndsAt`, ou `StartsAt + 6 h` quando não há término. Ele existe para "próximo" e "já rolou" virarem uma comparação indexada (`EndsAtOrDefault >= agora`) em vez de uma expressão no `WHERE`.
- **`EventGenre (EventId, Genre)`**: até 5, mesmo desenho de `BandGenre`.
- **`EventLineupEntry`** (`EventId`, `BandId?`, `Name?`, `Order`): banda cadastrada (`BandId`, com link e o evento aparecendo na página da banda) **ou** nome livre (`Name`, até 100), para quem não está no Noise Vault, inclusive banda famosa de fora.
  - Até 30 entradas, sem repetir banda; a ordem é a do formulário.
  - Editar **substitui** o line-up: as linhas antigas são apagadas e as novas inseridas na mesma transação. Nada referencia uma entrada.
- **`EventInterest (EventId, UserId, CreatedAt)`**: chave composta, sem `EntityBase`, pelo mesmo motivo do `PostLike`.
- **`EventComment`**: tabela própria, como `dominio.md` previa ("comentário tem um pai"), com soft delete.

## Próximo, já rolou, cancelado

- **Já rolou** = `EndsAtOrDefault < agora`. É calculado, não é status.
- `scope=upcoming` (padrão) ordena por `StartsAt` crescente; `scope=past`, por `StartsAt` decrescente.
- **Cancelado** continua nas duas listas, com o selo, para quem marcou "Vou" ficar sabendo. Não aceita interesse, comentário nem edição (`EVENT_CANCELLED`). O cancelamento é uma transição condicional `Published → Cancelled` no `WHERE` (`IEventRepository.TryCancelAsync`).
- **Apagar** é soft delete do evento e do post automático, e o banner sai do storage.

## Post automático no feed

- Ao criar o evento, na **mesma transação**, nasce um `Post` com `EventId` (único entre os posts), texto vazio, já publicado, com o mesmo autor e a mesma banda. Quem cria o post é `IPostAppService.CreateForEvent`, o dono do post.
- No feed, `PostDto.event` (`EventSummaryDto`) é lido **ao vivo**: cancelar ou editar o evento muda o card do feed sem tocar no post.
- Apagar o evento apaga o post (`DeleteForEvent`). Apagar só o post tira o evento do feed, mas o evento continua na agenda.
- **Exceção registrada à regra "injete o AppService dono"**: o `PostAppService` monta o card lendo `IEventRepository.ListByIdsAsync`, não o `IEventAppService`, porque o `EventAppService` já depende do `IPostAppService` (`CreateForEvent`) e o caminho contrário fecharia um ciclo de DI. O `BandAppService` lê `CountUpcomingByBandAsync` pelo mesmo motivo.

## Quem pode o quê

| Ação | Quem |
|---|---|
| Ver, filtrar, "Vou", comentar | qualquer pessoa logada |
| Criar | qualquer pessoa; como banda, só integrante (`NOT_BAND_MEMBER`) |
| Editar, banner, cancelar, apagar | o organizador, ou um **Owner** da banda organizadora (`EVENT_FORBIDDEN`) |
| Apagar comentário | quem comentou, ou quem pode gerenciar o evento |

A checagem fica no `WHERE` (`OrganizerUserId == eu` ou `Band.Members.Any(Owner == eu)`). O `canManage` do DTO sai da mesma regra.

## Validação

| Regra | Code |
|---|---|
| título 3–120 | `INVALID_EVENT_TITLE` |
| descrição até 5000 | `INVALID_EVENT_DESCRIPTION` |
| término depois do início e no máximo 7 dias depois (festival) | `INVALID_EVENT_DATES` |
| ao criar, começa no futuro, com 1 h de tolerância | `EVENT_IN_PAST` |
| local 2–120 | `INVALID_EVENT_VENUE` |
| cidade da lista; `Other` exige nome de 2 a 80 caracteres | `INVALID_CITY`, `INVALID_EVENT_CITY_NAME` |
| até 5 gêneros | `INVALID_GENRE` |
| link de ingresso http(s), até 500 | `INVALID_TICKETS_URL` |
| preço de 0 a 100.000 (2 casas) | `INVALID_EVENT_PRICE` |
| lotação de 1 a 200.000 | `INVALID_EVENT_CAPACITY` |
| line-up: até 30, banda ou nome, sem repetir banda | `INVALID_LINEUP` |
| banda do line-up existe | `LINEUP_BAND_NOT_FOUND` |
| até 20 eventos futuros ativos por pessoa | `EVENT_LIMIT_REACHED` (409) |

A data chega em ISO UTC do front (`datetime-local` convertido no navegador) e é gravada em UTC.

## Endpoints

| Método e rota | Code de sucesso |
|---|---|
| `GET /api/events?scope=&genre=&city=&search=&interested=` (paginado) | `EVENTS_FOUND` |
| `GET /api/events/upcoming?take=3` (rail do feed, até 10, sem paginação) | `EVENTS_FOUND` |
| `GET /api/bands/{slug}/events?scope=` (organizados pela banda ou em que ela está no line-up) | `EVENTS_FOUND` |
| `GET /api/users/{nickname}/events` (organizados pela pessoa) | `EVENTS_FOUND` |
| `POST /api/events` (`CreateEventInputDto`: dados, `lineup[]`, `bandSlug?`) | `EVENT_CREATED` |
| `GET /api/events/{id}` | `EVENT_FOUND` |
| `PUT /api/events/{id}` | `EVENT_UPDATED` |
| `POST /api/events/{id}/cancel` | `EVENT_CANCELLED_OK` |
| `DELETE /api/events/{id}` | `EVENT_DELETED` |
| `PUT` / `DELETE /api/events/{id}/interest` (idempotentes) | `EVENT_INTEREST_ADDED` / `EVENT_INTEREST_REMOVED` |
| `POST`/`DELETE /api/events/{id}/banner`, `PATCH …/banner-position` | os codes de capa (`BANNER_UPDATED`…) |
| `GET`/`POST /api/events/{id}/comments` | `EVENT_COMMENTS_FOUND` / `EVENT_COMMENT_CREATED` |
| `DELETE /api/event-comments/{id}` | `EVENT_COMMENT_DELETED` |

- **Busca** (`search`): título, local, nome da cidade livre, nome livre do line-up ou nome de banda cadastrada no line-up, em minúsculas com `Contains`.
- **Interesse** não vale em evento cancelado (`EVENT_CANCELLED`) nem em evento que já rolou (`EVENT_ENDED`); tirar o interesse vale sempre.
- **Banner**: preset `Banner` do `ImageReplacement`, chave `events/{id}/banner/{guid}.webp` (`armazenamento.md`). O front cria o evento e sobe o banner logo depois, na mesma ação.
- **Banda no hero**: `BandProfileDto.upcomingEventCount` conta só eventos publicados que ainda não rolaram.
- **Busca de bandas para o line-up**: `GET /api/bands/search` (`bandas.md`).

## Limites

| Política | Limite | Onde |
|---|---|---|
| `event-create` | 10/h por conta | `POST /api/events` (validações recusadas também contam) |
| `comment-create` | 60/h por conta, **compartilhado** com o feed | comentários de evento |
| `profile-upload` | 20/h por conta, compartilhado | banner do evento |

## Eventos de log (contexto `Events`)

| Evento | Nível | Dados |
|---|---|---|
| `EVENT_CREATED` | Info | `EventId`, `BandId`, `Lineup` |
| `EVENT_UPDATED` | Info | `EventId` |
| `EVENT_CANCELLED` | Info | `EventId` |
| `EVENT_DELETED` | Info | `EventId`, `ByBandOwner` |
| `EVENT_COMMENT_DELETED` | Info | `CommentId`, `EventId`, `ByOrganizer` |
| `BANNER_UPDATED` / `BANNER_REMOVED` | Info | `EventId`, `Key` |

Interesse e comentário não logam: são volume alto e o dado está no banco.

## Roteiro de verificação

Com a API no ar; conta A (admin, Owner da Serra Negra), conta B (integrante Member) e conta C (fã). O script `events-api.mjs` (fora do repositório) cobre:

1. **Validação.** Data no passado → `EVENT_IN_PAST`; término antes ou mais de 7 dias depois → `INVALID_EVENT_DATES`; `Other` sem nome → `INVALID_EVENT_CITY_NAME`; banda inexistente ou repetida no line-up → `LINEUP_BAND_NOT_FOUND` / `INVALID_LINEUP`; link sem http → `INVALID_TICKETS_URL`; C como banda → `NOT_BAND_MEMBER`.
2. **Criar.** B cria "Iron Maiden" como Serra Negra com line-up `[Iron Maiden (nome), Serra Negra (banda)]` → `EVENT_CREATED`. O feed de C traz o post automático com `event` preenchido e texto vazio.
3. **Listas.**
   - A busca por "maiden", "serra negra" e "curitiba" acha os eventos certos; `city=7` só traz SP.
   - `/upcoming?take=3` traz no máximo 3.
   - `/api/bands/serra-negra/events` traz o evento do line-up, e `upcomingEventCount` passa a 1.
4. **Interesse.** C marca duas vezes → 1; o filtro `interested=true` traz só esse; desmarca duas vezes → 0.
5. **Já rolou.** Um evento com `StartsAt`/`EndsAtOrDefault` forçados no passado (psql) aparece só em `scope=past`, com `hasEnded`; "Vou" nele → `EVENT_ENDED`.
6. **Banner.** B sobe → WebP no LocalStack; reposiciona; C sobe → `EVENT_FORBIDDEN`.
7. **Editar.** C → `EVENT_FORBIDDEN`; A (Owner da banda) → `EVENT_UPDATED`, com o line-up substituído na ordem nova.
8. **Comentários.** C e B comentam; C apaga o de B → `EVENT_COMMENT_FORBIDDEN`; B apaga o de C → `EVENT_COMMENT_DELETED`.
9. **Cancelar.** C → 403; B → `EVENT_CANCELLED_OK`; de novo, "Vou", comentar ou editar → `EVENT_CANCELLED`; o post do feed mostra `event.status = 1`.
10. **Apagar.** B apaga → o evento e o post automático respondem 404 e o banner some do storage.
