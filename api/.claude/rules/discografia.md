---
paths:
  - "**/NoiseVault.Application/Bands/BandDiscographyAppService.cs"
  - "**/NoiseVault.Application/Bands/DiscographyRules.cs"
  - "**/NoiseVault.Application/Bands/Dtos/BandDiscography*.cs"
  - "**/NoiseVault.Application/Interfaces/ISpotifyCatalog.cs"
  - "**/NoiseVault.Domain/Entities/BandRelease.cs"
  - "**/NoiseVault.Domain/Entities/BandTrack.cs"
  - "**/NoiseVault.Infra.Data/Spotify/**"
  - "**/NoiseVault.Workers/**/Discography/**"
---

# Decisões — Discografia da banda

Pedido do usuário em 16/10/2026: a página da banda mostra álbuns e músicas **sem a banda subir áudio de novo**. O Owner cola o link do artista no Spotify (o campo `socialLinks.spotify` que já existia) e a API importa álbuns, EPs, singles e faixas; a banda ordena, esconde e escolhe a faixa em destaque; quem não está no Spotify cadastra à mão. O som vem pelo player embutido do Spotify, que não exige chave e toca a prévia para quem não está logado lá.

## Modelo

- **`Band`** ganhou `SpotifyArtistId?` (22 caracteres, gravado na primeira importação) e `DiscographySyncedAt?`.
- **`BandRelease`** (`EntityBase`): `BandId`, `Title` (1–200), `Kind` (`EReleaseKind`: `Album` 0, `Ep` 1, `Single` 2), `Year` (1950 até o ano atual), `ReleaseDate?` (só quando o Spotify informa o dia), `CoverUrl?` (capa do Spotify, pela URL deles) **ou** `CoverKey?` (manual, no S3, `bands/{bandId}/releases/{guid}.webp`, preset `Avatar`: quadrado de 512), `Source` (`EReleaseSource`: `Spotify` 0, `Manual` 1), `SpotifyAlbumId?`, `ExternalUrl?`, `Order`, `IsHidden`. Índice `(BandId, Order)` e único filtrado `(BandId, SpotifyAlbumId) WHERE SpotifyAlbumId IS NOT NULL AND IsDeleted = false`.
- **`BandTrack`** (`EntityBase`): `BandReleaseId`, `Title`, `DiscNumber`, `Number`, `DurationSeconds?`, `SpotifyTrackId?`, `ExternalUrl?`, `IsFeatured` (uma por banda, garantida pelo AppService ao marcar).
- Por que entidade própria e não JSON na banda: ordem, esconder, destaque e capa manual são edições por item, e a faixa em destaque precisa de id estável para o front.
- Apagar lançamento é `SoftDelete` do `BandRelease` e `DeleteAsync` das faixas (nada referencia faixa). A capa manual é apagada do S3 na hora.

## Spotify: modo desenvolvimento, cópia no banco

- O app no painel de desenvolvedor do Spotify ("A New Level Music", do projeto antigo, renomeado para o Noise Vault) fica em **modo desenvolvimento**. Desde 2025 o modo estendido só é concedido a organizações com uso comprovado; para o nosso uso isso não importa, porque só lemos catálogo público com **client credentials** (sem login de usuário, sem a lista de 25 contas do painel) e **copiamos para o banco**. A página da banda nunca chama o Spotify: só o botão "Atualizar" e o job diário.
- `Spotify:ClientId` e `Spotify:ClientSecret` vêm **só de variável de ambiente** (`Spotify__ClientId`, `Spotify__ClientSecret`); o `appsettings.json` tem os dois vazios e `Spotify:Market = BR`. Sem eles o boot avisa no console, `ISpotifyCatalog.IsConfigured` é falso, a importação responde `SPOTIFY_NOT_CONFIGURED` e o job não faz nada. O resto da discografia (manual, ordem, destaque) funciona igual. O segredo já apareceu no projeto antigo: foi girado no painel antes de entrar aqui.
- **`ISpotifyCatalog`** (`Application/Interfaces/`) é a abstração; **`SpotifyCatalogClient`** (`Infra.Data/Spotify/`) é o primeiro **typed `HttpClient`** do projeto (`AddHttpClient<ISpotifyCatalog, SpotifyCatalogClient>`, timeout de 20 s). O token de client credentials fica no `SpotifyTokenCache` (singleton, renovado 60 s antes de vencer, com `SemaphoreSlim` para uma renovação por vez). 401 renova o token uma vez; 429 espera o `Retry-After` (teto de 10 s) uma vez e depois falha com `RateLimited`; 404 e 400 viram `NotFound`; o resto, `Unavailable`. O cliente devolve records (`SpotifyAlbum`, `SpotifyTrack`), nunca JSON cru, e o log `SPOTIFY_REQUEST_FAILED` leva só o caminho e o status, nunca o token.
- Chamadas: `GET /v1/artists/{id}/albums?include_groups=album,single&market=BR&limit=50` com paginação por `next`, e `GET /v1/albums?ids=` em lotes de 20 (cada álbum traz até 50 faixas; o resto vem por `GET /v1/albums/{id}/tracks`). Coletâneas e "aparece em" ficam de fora de propósito.
- **O Spotify não devolve mais `preview_url`** para apps criados depois de novembro de 2024. O áudio é o embed oficial por faixa ou álbum (`open.spotify.com/embed/...`), montado no front, que não precisa de chave.
- **Política de uso do conteúdo** (resumo do que seguimos): metadados em cache renovados (o job diário), capas servidas pela URL do Spotify sem recorte nem alteração (por isso `CoverUrl` e não cópia no S3), atribuição com o logo e o link para o artista no front, nunca áudio hospedado por nós, e **não misturar** conteúdo do Spotify com o de outro serviço na mesma faixa: lançamento importado só aponta para o Spotify, e manual só para o link próprio.

## Importação: dedupe, upsert, o que a banda editou fica

`POST /api/bands/{slug}/discography/import` (Owner, `discography-import` 5/h):

1. O link da banda precisa ser **do artista** (`DiscographyRules.ParseSpotifyArtistId`: `open.spotify.com/artist/{id}`, com ou sem `intl-xx/`, ou `spotify:artist:{id}`). Link de álbum, faixa ou playlist → `SPOTIFY_ARTIST_LINK_REQUIRED`. O id vai para `Band.SpotifyArtistId`, que o job usa depois.
2. **Dedupe** por título normalizado + ano (o Spotify repete o mesmo disco por mercado ou edição): fica o de mais faixas, desempate pelo id. `album_type = single` com 4 faixas ou mais vira `Ep`.
3. **Upsert por `SpotifyAlbumId`**, numa transação: lançamento existente recebe título, capa, data e faixas novas, mas **mantém `Order`, `IsHidden` e a faixa em destaque** (reencontrada pelo `SpotifyTrackId`); lançamento novo entra no fim da ordem, do mais recente para o mais antigo entre os novos; lançamento Spotify que sumiu do catálogo é apagado; manual não é tocado. Teto de 50 lançamentos por banda (contando os manuais) e 100 faixas por lançamento.
4. Log `DISCOGRAPHY_IMPORTED { BandId, Added, Updated, Removed }`.

O job `discography:refresh` (fila `default`, diário às 5 h) refaz o passo 3 para as bandas com `SpotifyArtistId` e sync com mais de 24 h, uma por vez com pausa de 1 s, e para no primeiro `RateLimited` (`DISCOGRAPHY_REFRESH_RATE_LIMITED`); falha de uma banda não derruba as outras (`DISCOGRAPHY_REFRESH_FAILED`).

## Quem pode o quê

| Ação | Quem |
|---|---|
| Ver a discografia (sem os escondidos) | qualquer pessoa logada |
| Ver os escondidos, importar, cadastrar, editar e apagar manual, trocar capa, reordenar, esconder, destacar | Owner |

Lançamento importado do Spotify **não se edita nem se apaga** (`RELEASE_NOT_EDITABLE`, 409): a fonte é o catálogo, e a importação seguinte sobrescreveria. Esconder e reordenar valem para os dois.

`GET /api/bands/{slug}/discography` **não é paginado** de propósito, como `suggested` e `mine` (`bandas.md`): o teto de 50 lançamentos é a regra de volume, e a página mostra tudo de uma vez. `canEdit` e `canImport` (Owner, Spotify configurado e link de artista) são só UX; quem protege é o `WHERE` de Owner.

## Endpoints

| Método e rota | Code de sucesso |
|---|---|
| `GET /api/bands/{slug}/discography` → `BandDiscographyDto { syncedAt, spotifyArtistId, spotifyArtistUrl, canEdit, canImport, featuredTrack?, releases[] }` | `DISCOGRAPHY_FOUND` |
| `POST /api/bands/{slug}/discography/import` (`discography-import`) | `DISCOGRAPHY_IMPORTED` |
| `POST /api/bands/{slug}/discography/releases { title, kind, year, externalUrl?, tracks[] { title, externalUrl?, durationSeconds? } }` | `RELEASE_CREATED` |
| `PUT /api/bands/{slug}/discography/releases/{id}` (só manual; substitui as faixas, mantendo o destaque pelo título) | `RELEASE_UPDATED` |
| `DELETE /api/bands/{slug}/discography/releases/{id}` (só manual) | `RELEASE_DELETED` |
| `POST /api/bands/{slug}/discography/releases/{id}/cover` (só manual, `profile-upload`, preset `Avatar`) | `RELEASE_COVER_UPDATED` |
| `PUT /api/bands/{slug}/discography/order { releaseIds[] }` (todos, uma vez cada) | `DISCOGRAPHY_REORDERED` |
| `PATCH /api/bands/{slug}/discography/releases/{id}/visibility { hidden }` | `RELEASE_VISIBILITY_UPDATED` |
| `PUT /api/bands/{slug}/discography/featured { trackId? }` (nulo limpa) | `FEATURED_TRACK_UPDATED` |

**Erros**: `RELEASE_NOT_FOUND` e `TRACK_NOT_FOUND` (404); `RELEASE_NOT_EDITABLE` e `DISCOGRAPHY_LIMIT_REACHED` (409); `INVALID_RELEASE_TITLE`, `INVALID_RELEASE_KIND`, `INVALID_RELEASE_YEAR`, `INVALID_TRACK_TITLE` (também para mais de 100 faixas), `INVALID_RELEASE_ORDER`, `INVALID_EXTERNAL_URL` (http(s), até 500) e `SPOTIFY_ARTIST_LINK_REQUIRED` (400); `SPOTIFY_NOT_CONFIGURED` e `SPOTIFY_UNAVAILABLE` (500), `SPOTIFY_ARTIST_NOT_FOUND` (404), `SPOTIFY_RATE_LIMITED` (409). Reaproveitados: `BAND_NOT_FOUND`, `BAND_FORBIDDEN`, `IMAGE_*`.

## Eventos de log (contexto `Bands`)

| Evento | Nível | Dados |
|---|---|---|
| `DISCOGRAPHY_IMPORTED` | Info | `BandId`, `Added`, `Updated`, `Removed` |
| `DISCOGRAPHIES_REFRESHED` | Info | `Refreshed`, `Candidates` (job) |
| `DISCOGRAPHY_REFRESH_FAILED` | Warning | `BandId`, `Failure`, `Message` |
| `DISCOGRAPHY_REFRESH_RATE_LIMITED` | Warning | `Refreshed`, `Pending` |
| `RELEASE_CREATED` / `RELEASE_DELETED` | Info | `BandId`, `ReleaseId` (+ `Tracks`) |
| `SPOTIFY_REQUEST_FAILED` | Error | `Path`, `Status` (nunca token nem corpo) |

## Roteiro de verificação

Conta A (Owner de uma banda) e conta C (qualquer). Para o Spotify, `Spotify__ClientId` e `Spotify__ClientSecret` como variáveis de ambiente da API.

1. **Sem chave.** Boot avisa; `POST …/discography/import` → `SPOTIFY_NOT_CONFIGURED`; `GET …/discography` → `canImport: false`.
2. **Link errado.** Com as chaves, a banda com `spotify` apontando para um álbum → `SPOTIFY_ARTIST_LINK_REQUIRED`; com `https://open.spotify.com/intl-pt/artist/{id}` → `DISCOGRAPHY_IMPORTED`, `releases` com capa, `kind`, `year`, faixas com `spotifyTrackId` e duração; `DISCOGRAPHY_IMPORTED` no log com `Added`.
3. **Edição que sobrevive.** A esconde um lançamento, move outro para o topo e marca uma faixa em destaque; importar de novo → `Updated` igual ao total, `Added: 0`, e os três ajustes continuam. C vê a lista sem o escondido e sem `canEdit`.
4. **Manual.** A cadastra "Demo 98" (EP, 1998, 3 faixas com link do YouTube) → `RELEASE_CREATED`; sobe a capa → `RELEASE_COVER_UPDATED` e a URL `bands/{id}/releases/…webp` responde 200; edita trocando uma faixa → `RELEASE_UPDATED`; apaga → `RELEASE_DELETED` e a capa responde 404. Apagar um importado → `RELEASE_NOT_EDITABLE`. `order` faltando um id → `INVALID_RELEASE_ORDER`.
5. **Limites.** O 6º import na hora → 429; o 51º lançamento → `DISCOGRAPHY_LIMIT_REACHED`.
6. **Job.** `discography:refresh` disparado pelo `/hangfire` atualiza as bandas com sync antigo e registra `DISCOGRAPHIES_REFRESHED`.
