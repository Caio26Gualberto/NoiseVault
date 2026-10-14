---
paths:
  - "**/NoiseVault.Application/Archive/**"
  - "**/NoiseVault.Infra.Data/Repositories/Archive/**"
  - "**/NoiseVault.Api/Controllers/ArchiveController.cs"
  - "**/NoiseVault.Api/Controllers/Admin/ArchiveAdminController.cs"
  - "**/NoiseVault.Domain/Entities/Archive*.cs"
---

# Decisões — Mural das antigas

Módulo 5 da ordem em `dominio.md`, **redefinido pelo usuário em 13/10/2026**. O módulo previsto era "Galeria e moderação": fotos e vídeos do YouTube abertos à comunidade, com fila de aprovação. Ele caiu pelo seguinte:
- a galeria existia no legado como atalho para usar o YouTube;
- com o feed aceitando foto e vídeo de verdade (`feed.md`), ela viraria um segundo feed, mais fraco.

No lugar entra o **mural das antigas**: o arquivo da cena do ABC, com fotos e fitas VHS dos anos 80 e 90 digitalizadas e contadas por quem viveu. **Na largada, só o dono do projeto publica.** A comunidade comenta, curte e marca "Eu estava lá".

## Modelo

- **`ArchiveEntry`**, o "registro", que é uma memória:
  - `AuthorUserId` (quem publicou, sempre alguém com permissão), `Title` (3–150), `Description?` (até 10.000, para contar a história);
  - data aproximada: `Year` (1950 até o ano atual), `Month?` e `Day?`. Dia exige mês, e o dia tem que existir no mês. É a **precisão** da memória: "1987", "agosto de 1987" ou "15 de agosto de 1987";
  - `SortDate`, gravado pelo AppService (o dia 1 completa o que falta), para ordenar;
  - `Venue?`, `City?` (`EAbcCity`) e `CityName?` (obrigatório com `Other`);
  - `CoverMediaUploadId`: a capa. Se não for escolhida, ou se a mídia escolhida sair do registro, é a primeira mídia.
- **`ArchiveBandEntry`** (`BandId?`, `Name?`, `Order`): banda cadastrada ou nome livre, até 30. Mesmo desenho do line-up de evento (`eventos.md`).
- **`ArchiveMedia`** (`MediaUploadId` único, `Order`, `Caption?` até 500): de 1 a **100** mídias por registro, fotos e vídeos misturados, com legenda em cada uma ("Andreas na guitarra").
- **`ArchiveLike`**, **`ArchiveAttendance`** ("Eu estava lá") e **`ArchiveComment`**: cada um tem a sua tabela ("comentário tem um pai", `dominio.md`).
- **`Post.ArchiveEntryId`** (único): o **post automático** no feed, criado na mesma transação do registro e apagado junto, como o do evento (`feed.md`).
- Um registro com vídeo ainda processando **sai publicado**: quem publica é a curadoria, não há o que esconder. O card mostra "processando" naquela mídia até ela ficar pronta, e o detalhe consulta de novo a cada 5 s.

## Quem pode o quê

| Ação | Quem |
|---|---|
| Ver mural, linha do tempo, registro, quem estava lá | qualquer pessoa logada |
| Curtir, "Eu estava lá", comentar | qualquer pessoa logada |
| Subir mídia de arquivo, criar registro | `Archive.Create` |
| Editar qualquer registro (dados, bandas, mídias, legendas, ordem, capa) | `Archive.Edit` |
| Apagar registro e comentário de qualquer pessoa | `Archive.Delete` |
| Apagar o próprio comentário | quem comentou |

- **Permissões do catálogo, não "só o Caio".** O grupo é "Mural das antigas" (`PermissionCatalog`). Nenhum papel padrão recebe as permissões; o `Admin` tem todas, implícitas. Um papel "Curador" criado pelo painel passa a publicar sem mudar código.
- **A edição é coletiva**: quem tem `Archive.Edit` edita qualquer registro, não só os seus. O arquivo é da cena, não de quem subiu.
- A escrita fica em `Controllers/Admin/ArchiveAdminController.cs` (`api/admin/archive`), com `[Authorize(Policy)]` em cada action. A leitura e a interação ficam em `Controllers/ArchiveController.cs`.
- `canEdit` e `canDelete` no DTO saem do `IPermissionChecker.IsGrantedAsync`. A tela usa isso só para UX; quem protege é a policy.

## Mídia de arquivo

As mídias usam o mesmo pipeline do feed (upload em partes, HLS, poster), com o **perfil `Archive`** no `MediaUpload` (`midia-e-video.md`):
- foto até **40 MB**, saída com lado maior até **3072 px**;
- vídeo até **4 GB** e **2 h** (uma fita VHS inteira);
- prazo de processamento de 8 h.

O upload é feito por `POST /api/admin/archive/media/photos|videos` (`Archive.Create`). Concluir, consultar e apagar a mídia reaproveitam `/api/media`. Mídia do perfil `Post` não entra num registro, e vice-versa (`MEDIA_NOT_ATTACHABLE`).

**Arquivo de mídia** (`media:cleanup`): o vínculo `MediaUpload.ArchiveAttachment` impede a limpeza de tratar a mídia do mural como solta. Sem ele, o job apagaria as fotos do mural depois de 24 h. Apagar o registro apaga os arquivos na hora, e a limpeza zera as chaves depois.

**Edição de mídias**: o `PUT` manda a lista final, na ordem, com as legendas e a capa:
- mídias novas precisam estar soltas, ser do perfil `Archive` e ser de quem edita;
- as que saíram da lista têm a linha e os arquivos apagados (`IMediaUploadAppService.Discard`), **depois** do commit.

## Leitura

- `GET /api/archive` é paginado, com os filtros:
  - `year`, ou `decade` (1980 = 1980–1989);
  - `search` (título, história, local, cidade livre, banda cadastrada ou nome livre);
  - `city` e `kind` (`photo`/`video`: tem pelo menos uma mídia do tipo);
  - `sort` (`newest` padrão, `oldest`), pela `SortDate` com desempate por `Id`.
- `GET /api/archive/years` → `[{ year, count }]` em ordem crescente, que alimenta a linha do tempo.
- `GET /api/archive/{id}`: o detalhe traz as mídias com legenda e as 5 primeiras pessoas de "quem estava lá". `GET …/attendees` pagina o resto.
- `GET /api/bands/{slug}/archive`: os registros em que a banda aparece.
- **Feed**: o `PostDto.archive` (`ArchiveSummaryDto`: título, data, capa e contagens) é lido em lote do `IArchiveRepository`, com a mesma exceção de DI registrada para o evento.

## Endpoints

| Método e rota | Code de sucesso |
|---|---|
| `GET /api/archive` · `GET /api/bands/{slug}/archive` | `ARCHIVE_ENTRIES_FOUND` |
| `GET /api/archive/years` | `ARCHIVE_YEARS_FOUND` |
| `GET /api/archive/{id}` | `ARCHIVE_ENTRY_FOUND` |
| `GET /api/archive/{id}/attendees` | `ARCHIVE_ATTENDEES_FOUND` |
| `PUT`/`DELETE /api/archive/{id}/like` | `ARCHIVE_LIKED` / `ARCHIVE_UNLIKED` |
| `PUT`/`DELETE /api/archive/{id}/attendance` | `ARCHIVE_ATTENDED` / `ARCHIVE_UNATTENDED` |
| `GET`/`POST /api/archive/{id}/comments` · `DELETE /api/archive-comments/{id}` | `ARCHIVE_COMMENTS_FOUND` / `ARCHIVE_COMMENT_CREATED` / `ARCHIVE_COMMENT_DELETED` |
| `POST /api/admin/archive` (`Archive.Create`) | `ARCHIVE_ENTRY_CREATED` |
| `PUT /api/admin/archive/{id}` (`Archive.Edit`) | `ARCHIVE_ENTRY_UPDATED` |
| `DELETE /api/admin/archive/{id}` (`Archive.Delete`) | `ARCHIVE_ENTRY_DELETED` |
| `POST /api/admin/archive/media/photos` · `…/videos` (`Archive.Create`) | `PHOTO_UPLOADED` / `VIDEO_UPLOAD_STARTED` |

**Erros:**
- Registro: `ARCHIVE_ENTRY_NOT_FOUND`.
- Validação do formulário:
  - `INVALID_ARCHIVE_TITLE`, `INVALID_ARCHIVE_DESCRIPTION`, `INVALID_ARCHIVE_DATE`;
  - `INVALID_ARCHIVE_VENUE`, `INVALID_ARCHIVE_CITY_NAME`, `INVALID_ARCHIVE_CAPTION`.
- Mídia: `ARCHIVE_MEDIA_REQUIRED` e `TOO_MANY_ARCHIVE_MEDIA`.
- Comentário: `ARCHIVE_COMMENT_NOT_FOUND` e `ARCHIVE_COMMENT_FORBIDDEN`.
- Reaproveitados: `INVALID_LINEUP`, `LINEUP_BAND_NOT_FOUND`, `MEDIA_NOT_ATTACHABLE`, `INVALID_CITY`, `INVALID_COMMENT_TEXT`, `IMAGE_TOO_LARGE`, `VIDEO_TOO_LARGE`.

**Limites**:
- O upload reaproveita `media-photo` e `media-video`, e o comentário, `comment-create`.
- Não há limite de registros por pessoa: só quem tem permissão publica.

## Eventos de log (contexto `Archive`)

| Evento | Nível | Dados |
|---|---|---|
| `ARCHIVE_ENTRY_CREATED` | Info | `EntryId`, `Year`, `Media`, `Bands` |
| `ARCHIVE_ENTRY_UPDATED` | Info | `EntryId`, `Media`, `RemovedMedia` |
| `ARCHIVE_ENTRY_DELETED` | Info | `EntryId`, `Media` |
| `ARCHIVE_COMMENT_DELETED` | Info | `CommentId`, `ArchiveEntryId`, `ByCurator` |

## Roteiro de verificação

Com a API e o ffmpeg no ar; conta A (admin) e conta C (fã). O script `archive-api.mjs` vive fora do repositório.

1. **Permissão.** C → `POST /api/admin/archive` e `…/media/videos` → 403. A consegue.
2. **Perfil.**
   - Uma foto de cerca de 30 MB passa em `…/archive/media/photos` e é recusada no `/api/media/photos` (`IMAGE_TOO_LARGE`).
   - O start de 3 GB passa no arquivo e é recusado no feed (`VIDEO_TOO_LARGE`).
3. **VHS.** Um vídeo 480i entrelaçado converte em 360p e 480p, e o log `VIDEO_TRANSCODED` mostra `Deinterlaced = true` e `Profile = Archive`.
4. **Criar.**
   - Registro "agosto de 1987" com fotos legendadas, vídeo, bandas misto e capa → `ARCHIVE_ENTRY_CREATED`; o feed traz o post com `archive`.
   - Sem mídia → `ARCHIVE_MEDIA_REQUIRED`; dia sem mês → `INVALID_ARCHIVE_DATE`; mídia do feed → `MEDIA_NOT_ATTACHABLE`.
5. **Linha do tempo.** `years` traz 1987; `year=1987` e `decade=1980` filtram; busca por banda e por local acha.
6. **Interação.**
   - Curtir e "Eu estava lá" duas vezes contam 1, e `attendees` lista C.
   - C comenta; C apaga o comentário de A → 403; A apaga o de C.
7. **Editar.** Reordenar, trocar a capa e a legenda, tirar uma foto (que passa a responder 404 no storage) e incluir outra. C → 403.
8. **Limpeza.** Com `ExpiresAt` das mídias do registro forçado no passado, o `media:cleanup` não apaga nada.
9. **Apagar.** O registro e o post somem, e as mídias respondem 404.
