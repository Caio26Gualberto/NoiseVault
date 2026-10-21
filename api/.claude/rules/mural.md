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

No lugar entra o **mural das antigas**: o arquivo da cena do ABC, com fotos e fitas VHS dos anos 80 e 90 digitalizadas e contadas por quem viveu. Na largada só o dono do projeto publicava; desde 14/10/2026 **a comunidade também envia**, e a curadoria aprova (ideia 2 do `IDEIAS.md`, "Envio da comunidade" abaixo). Todo mundo comenta, curte e marca "Eu estava lá".

## Modelo

- **`ArchiveEntry`**, o "registro", que é uma memória:
  - `AuthorUserId` (quem publicou, ou quem enviou da comunidade), `Title` (3–150), `Description?` (até 10.000, para contar a história);
  - `Status` (`EArchiveEntryStatus`: `Published` 0, `Pending` 1, `Rejected` 2), `IsSubmission` (nasceu de envio da comunidade; continua `true` depois de aprovado), `ReviewedByUserId?`, `ReviewedAt?` e `RejectionReason?` (3–500);
  - data aproximada: `Year` (1950 até o ano atual), `Month?` e `Day?`. Dia exige mês, e o dia tem que existir no mês. É a **precisão** da memória: "1987", "agosto de 1987" ou "15 de agosto de 1987";
  - `SortDate`, gravado pelo AppService (o dia 1 completa o que falta), para ordenar;
  - `Venue?`, `CityId?` (município do IBGE) ou `CityName?` (fora do Brasil), opcionais (`cidades.md`);
  - `CoverMediaUploadId`: a capa. Se não for escolhida, ou se a mídia escolhida sair do registro, é a primeira mídia.
- **`ArchiveBandEntry`** (`BandId?`, `Name?`, `Order`): banda cadastrada ou nome livre, até 30. Mesmo desenho do line-up de evento (`eventos.md`).
- **`ArchiveMedia`** (`MediaUploadId` único, `Order`, `Caption?` até 500): de 1 a **100** mídias por registro, fotos e vídeos misturados, com legenda em cada uma ("Andreas na guitarra").
- **`ArchiveLike`**, **`ArchiveAttendance`** ("Eu estava lá") e **`ArchiveComment`**: cada um tem a sua tabela ("comentário tem um pai", `dominio.md`).
- As histórias (comentários) aceitam @menção, e as fotos do registro publicado aceitam marcação de quem aparece (`marcacoes.md`). A marcação é presa ao `MediaUploadId`, então sobrevive à edição do registro.
- **`Post.ArchiveEntryId`** (único): o **post automático** no feed, criado na mesma transação da publicação (do registro da curadoria, ou da aprovação do envio) e apagado junto, como o do evento (`feed.md`).
- Um registro com vídeo ainda processando **sai publicado**: quem publica ou aprova é a curadoria, não há o que esconder. O card mostra "processando" naquela mídia até ela ficar pronta, e o detalhe consulta de novo a cada 5 s.

## Envio da comunidade

Quem tem as fotos e fitas são os membros do grupo do Facebook, não a curadoria. Decisões do usuário em 14/10/2026: a curadoria **edita e publica** ou **recusa com motivo** (não existe "devolver para ajuste"); o registro aprovado sai **em nome de quem enviou**; os limites de mídia são os mesmos do perfil `Archive`; até **5 envios pendentes** por pessoa.

- **O envio é um `ArchiveEntry` com `Status = Pending`**, não uma entidade paralela: bandas, mídias, legendas e capa são as mesmas, a edição da curadoria reaproveita o `Update`, e aprovar só vira o status e cria o post.
- **Visibilidade**:
  - lista, linha do tempo, banda e resumo do feed só enxergam `Published` (o `Visible()` do `ArchiveRepository` filtra);
  - o detalhe abre também para quem enviou e para quem tem `Archive.Review` (`FindReadable`). Para qualquer outra pessoa o pendente não existe (`ARCHIVE_ENTRY_NOT_FOUND`);
  - curtir, "Eu estava lá" e comentar exigem `Published`: quem enxerga o pendente recebe `ARCHIVE_ENTRY_NOT_PUBLISHED` (409).
- **O pendente não tem post.** O post nasce na aprovação, com `AuthorUserId` de quem enviou. Curtidas, comentários e "Eu estava lá" notificam essa pessoa, porque o acervo é dela.
- **Aprovar e recusar são atômicos**: `TryReviewAsync` é um `ExecuteUpdateAsync` com `WHERE Status = Pending AND NOT IsDeleted`, e zero linhas é a recusa (`ARCHIVE_SUBMISSION_NOT_PENDING`). Dois curadores agindo juntos: um ganha. Como `ExecuteUpdateAsync` não passa pelo `ApplyAuditing`, o repositório grava `ReviewedBy/At` e `UpdatedBy/At` à mão.
- **Recusar** exige motivo. Na mesma transação saem as linhas de `ArchiveMedia`; depois do commit as mídias são descartadas (`IMediaUploadAppService.Discard`). O registro recusado fica, com título, data e motivo, para a pessoa ver em "Meus envios".
- **Quem enviou** edita enquanto está `Pending` (`PUT /api/archive/submissions/{id}`) e apaga o pendente (retirar) ou o recusado. O publicado deixa de ser dela para editar: a edição volta a ser coletiva (`Archive.Edit`). ⚠️ **Risco aceito**: a edição de quem enviou é um `UPDATE` comum, sem guarda no `WHERE`; uma edição que cruza com a aprovação no mesmo instante passa. A janela é de milissegundos e o pior caso é o texto da pessoa sobre o da curadoria.
- **Crédito**: `IsSubmission` diz ao front para mostrar "Acervo de Fulano" em vez de "Publicado por".
- **Quem tem `Archive.Create`** continua publicando direto por `/api/admin/archive`. O envio é para quem não tem; se um curador usar o envio, o registro entra na fila como o de qualquer pessoa.
- ⚠️ **Risco a revisar no deploy**: qualquer pessoa sobe vídeo de até 2 h, e cada um custa uma conversão (CPU em dev; MediaConvert em produção, uns 2 USD por fita, `hospedagem.md`), mesmo que o envio seja recusado. O que segura: 3 vídeos em envio ou processamento por pessoa, `media-video` 10/h e os 5 pendentes.

## Quem pode o quê

| Ação | Quem |
|---|---|
| Ver mural, linha do tempo e registro publicado | **qualquer pessoa, sem login** (só leitura, sem as marcações das fotos nem os nomes de quem estava lá, desde 20/10/2026, `seo.md`) |
| Ver quem estava lá e as marcações | qualquer pessoa logada |
| Curtir, "Eu estava lá", comentar | qualquer pessoa logada |
| Subir mídia de arquivo e enviar um registro para a curadoria | qualquer pessoa logada |
| Editar e retirar o próprio envio pendente; apagar o próprio envio recusado | quem enviou |
| Criar registro já publicado | `Archive.Create` |
| Ver a fila de envios, publicar ou recusar | `Archive.Review` |
| Editar qualquer registro, inclusive o pendente (dados, bandas, mídias, legendas, ordem, capa) | `Archive.Edit` |
| Apagar registro e comentário de qualquer pessoa | `Archive.Delete` |
| Apagar o próprio comentário | quem comentou |

- **Permissões do catálogo, não "só o Caio".** O grupo é "Mural das antigas" (`PermissionCatalog`). Nenhum papel padrão recebe as permissões; o `Admin` tem todas, implícitas. Um papel "Curador" criado pelo painel passa a publicar sem mudar código.
- **A edição é coletiva**: quem tem `Archive.Edit` edita qualquer registro, não só os seus. O arquivo é da cena, não de quem subiu.
- A escrita fica em `Controllers/Admin/ArchiveAdminController.cs` (`api/admin/archive`), com `[Authorize(Policy)]` em cada action. A leitura e a interação ficam em `Controllers/ArchiveController.cs`.
- `Archive.Review` não declara pai e não implica `Archive.Edit`: um papel "Curador" leva as duas para revisar e ajustar.
- No DTO:
  - `canEdit` é `Archive.Edit`, ou ser quem enviou com o envio `Pending`;
  - `canDelete` sai de `Archive.Delete`, e `canReview` de `Archive.Review` com o registro `Pending`;
  - `isMine`, `status`, `isSubmission`, `rejectionReason` e `reviewedAt` completam.
  - A tela usa isso só para UX; quem protege é a policy.

## Mídia de arquivo

As mídias usam o mesmo pipeline do feed (upload em partes, HLS, poster), com o **perfil `Archive`** no `MediaUpload` (`midia-e-video.md`):
- foto até **40 MB**, saída com lado maior até **3072 px**;
- vídeo até **4 GB** e **2 h** (uma fita VHS inteira);
- prazo de processamento de 8 h.

O upload é feito por `POST /api/admin/archive/media/photos|videos` (`Archive.Create`) ou, para o envio da comunidade, por `POST /api/archive/media/photos|videos` (qualquer pessoa, mesmo perfil e mesmos limites). Concluir, consultar e apagar a mídia reaproveitam `/api/media`. Mídia do perfil `Post` não entra num registro, e vice-versa (`MEDIA_NOT_ATTACHABLE`).

**Arquivo de mídia** (`media:cleanup`): o vínculo `MediaUpload.ArchiveAttachment` impede a limpeza de tratar a mídia do mural como solta. Sem ele, o job apagaria as fotos do mural depois de 24 h. Apagar o registro apaga os arquivos na hora, e a limpeza zera as chaves depois.

**Edição de mídias**: o `PUT` manda a lista final, na ordem, com as legendas e a capa:
- mídias novas precisam estar soltas, ser do perfil `Archive` e ser de quem edita;
- as que saíram da lista têm a linha e os arquivos apagados (`IMediaUploadAppService.Discard`), **depois** do commit.

## Leitura

- `GET /api/archive` é paginado, com os filtros:
  - `year`, ou `decade` (1980 = 1980–1989);
  - `search` (título, história, local, cidade livre, banda cadastrada ou nome livre);
  - `cityId` e `kind` (`photo`/`video`: tem pelo menos uma mídia do tipo);
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
| `POST /api/archive/media/photos` · `…/videos` | `PHOTO_UPLOADED` / `VIDEO_UPLOAD_STARTED` |
| `POST /api/archive/submissions` (`archive-submit`) | `ARCHIVE_SUBMISSION_CREATED` |
| `GET /api/archive/submissions` (os meus, todos os status, mais novos primeiro) | `ARCHIVE_SUBMISSIONS_FOUND` |
| `PUT`/`DELETE /api/archive/submissions/{id}` (quem enviou) | `ARCHIVE_SUBMISSION_UPDATED` / `ARCHIVE_SUBMISSION_DELETED` |
| `GET /api/admin/archive/submissions?status=&search=` (`Archive.Review`) | `ARCHIVE_SUBMISSIONS_FOUND` |
| `GET /api/admin/archive/submissions/summary` (`Archive.Review`) → `{ pending, published, rejected }` | `ARCHIVE_SUBMISSION_SUMMARY_FOUND` |
| `POST /api/admin/archive/{id}/approve` (`Archive.Review`) | `ARCHIVE_SUBMISSION_APPROVED` |
| `POST /api/admin/archive/{id}/reject { reason }` (`Archive.Review`) | `ARCHIVE_SUBMISSION_REJECTED` |

A fila da curadoria (`status=1`) vem dos mais antigos para os mais novos, para ninguém ficar esquecido no fim; os outros status, dos mais novos para os mais antigos. A busca procura no título e no nome ou apelido de quem enviou.

**Erros:**
- Registro: `ARCHIVE_ENTRY_NOT_FOUND`.
- Validação do formulário:
  - `INVALID_ARCHIVE_TITLE`, `INVALID_ARCHIVE_DESCRIPTION`, `INVALID_ARCHIVE_DATE`;
  - `INVALID_ARCHIVE_VENUE`, `INVALID_ARCHIVE_CAPTION`, e os de cidade (`INVALID_CITY`, `INVALID_CITY_NAME`).
- Mídia: `ARCHIVE_MEDIA_REQUIRED` e `TOO_MANY_ARCHIVE_MEDIA`.
- Comentário: `ARCHIVE_COMMENT_NOT_FOUND` e `ARCHIVE_COMMENT_FORBIDDEN`.
- Envio: `ARCHIVE_SUBMISSION_LIMIT_REACHED` (409), `ARCHIVE_SUBMISSION_NOT_PENDING` (409, já revisado, ou quem enviou tentando editar ou apagar o publicado), `ARCHIVE_ENTRY_NOT_PUBLISHED` (409), `INVALID_ARCHIVE_REJECTION_REASON` e `INVALID_ARCHIVE_STATUS`.
- Reaproveitados: `INVALID_LINEUP`, `LINEUP_BAND_NOT_FOUND`, `MEDIA_NOT_ATTACHABLE`, `INVALID_CITY`, `INVALID_COMMENT_TEXT`, `IMAGE_TOO_LARGE`, `VIDEO_TOO_LARGE`.

**Limites**:
- ~~O upload reaproveita `media-photo`~~: a foto tem limite próprio desde 20/10/2026, `archive-photo` (300/h) na curadoria e `archive-submit-photo` (100/h) no envio da comunidade, porque um álbum de 100 fotos estourava os 60/h do feed (`abuso-e-limites.md`). O vídeo continua no `media-video`, e o comentário, no `comment-create`.
- O envio tem `archive-submit` (10/h por conta) e o limite de volume de **5 pendentes** por pessoa (`ARCHIVE_SUBMISSION_LIMIT_REACHED`, contado no `CountPendingAsync`). Registro publicado não conta: quem já foi aprovado pode mandar mais.
- Não há limite para a curadoria publicar direto.

## Eventos de log (contexto `Archive`)

| Evento | Nível | Dados |
|---|---|---|
| `ARCHIVE_ENTRY_CREATED` | Info | `EntryId`, `Year`, `Media`, `Bands` |
| `ARCHIVE_ENTRY_UPDATED` | Info | `EntryId`, `Media`, `RemovedMedia` |
| `ARCHIVE_ENTRY_DELETED` | Info | `EntryId`, `Media` |
| `ARCHIVE_COMMENT_DELETED` | Info | `CommentId`, `ArchiveEntryId`, `ByCurator` |
| `ARCHIVE_SUBMISSION_CREATED` | Info | `EntryId`, `Year`, `Media`, `Bands` |
| `ARCHIVE_SUBMISSION_UPDATED` | Info | `EntryId`, `Media`, `RemovedMedia` |
| `ARCHIVE_SUBMISSION_DELETED` | Info | `EntryId`, `Media` |
| `ARCHIVE_SUBMISSION_APPROVED` | Info | `EntryId`, `AuthorUserId` |
| `ARCHIVE_SUBMISSION_REJECTED` | Info | `EntryId`, `AuthorUserId`, `Media` (o motivo não entra: é texto livre) |

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

### Envio da comunidade

Conta A (admin), contas C e D (fãs). O script `archive-submissions-api.mjs` vive fora do repositório.

1. **Envio.** C sobe uma foto por `/api/archive/media/photos` e envia → `ARCHIVE_SUBMISSION_CREATED` com `status: 1`.
   - O envio não aparece em `/api/archive`, nos anos nem no feed.
   - D abre → `ARCHIVE_ENTRY_NOT_FOUND`; C abre; A abre com `canReview`. A recebe a notificação 20.
   - Mídia do feed → `MEDIA_NOT_ATTACHABLE`; C no `POST /api/admin/archive` → 403.
2. **Bloqueios.** Curtir ou comentar o pendente → `ARCHIVE_ENTRY_NOT_PUBLISHED`. C na fila ou no `approve` → 403. O 6º pendente → `ARCHIVE_SUBMISSION_LIMIT_REACHED`.
3. **Aprovação.** C edita o pendente; A edita pelo admin e aprova → `ARCHIVE_SUBMISSION_APPROVED`.
   - O registro entra no mural e nos anos, e o post sai no feed em nome de C. C recebe a 21 e, depois, a curtida de D.
   - Aprovar de novo → `ARCHIVE_SUBMISSION_NOT_PENDING`. C editar ou apagar o publicado → `ARCHIVE_SUBMISSION_NOT_PENDING`, e o `canEdit` de C vira `false`.
4. **Recusa.** Sem motivo → `INVALID_ARCHIVE_REJECTION_REASON`; com motivo → `ARCHIVE_SUBMISSION_REJECTED`.
   - A mídia responde 404 no storage; "meus envios" de C mostra o motivo, e C recebe a 22.
   - C apaga o recusado e retira um pendente; D apagar o de C → `ARCHIVE_ENTRY_NOT_FOUND`. Com vaga, C envia de novo.
5. **Limpeza.** Com `ExpiresAt` das mídias dos pendentes no passado, o `media:cleanup` não toca nelas; só a do envio retirado perde a chave (`OfDeletedPosts`).

Resultado em 14/10/2026: as 47 checagens do script passaram, e a limpeza foi conferida à mão.
