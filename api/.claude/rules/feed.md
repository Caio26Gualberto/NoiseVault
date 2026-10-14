---
paths:
  - "**/NoiseVault.Application/Posts/**"
  - "**/NoiseVault.Infra.Data/Repositories/Posts/**"
  - "**/NoiseVault.Api/Controllers/PostsController.cs"
  - "**/NoiseVault.Api/Controllers/CommentsController.cs"
  - "**/NoiseVault.Domain/Entities/Post*.cs"
  - "**/NoiseVault.Domain/Entities/Comment.cs"
---

# Decisões — Feed (posts, curtidas, comentários)

Módulo 3 da ordem em `dominio.md`. A mídia (fotos e vídeo) é de `midia-e-video.md`; aqui fica o que acontece com ela dentro de um post.

## Quem publica

- **O autor é sempre a pessoa** (`Post.AuthorUserId`). "Publicar como banda" grava `BandId`: o post aparece com o avatar e o nome da banda e "por @fulano" embaixo. Banda nunca tem login; a moderação sempre sabe quem publicou.
- **Qualquer integrante** (Owner ou Member) publica como a banda. Decisão do usuário em 13/10/2026: banda pequena costuma ter uma pessoa só cuidando da divulgação. Quem não é integrante recebe `NOT_BAND_MEMBER` (403).
- **Comentário é sempre da pessoa.** Responder "como banda" ficou fora do primeiro corte.

## Feed único e cronológico

- `GET /api/posts` lista tudo, do mais novo para o mais velho (`PublishedAt ?? CreatedAt`, desempate por `Id`). As abas "Tudo/Bandas" do mock morreram: sem "seguir" no domínio, uma aba "Seguindo" seria dado falso, e o usuário preferiu feed único.
- `GET /api/users/{nickname}/posts` lista o que a pessoa publicou (inclusive como banda). `GET /api/bands/{slug}/posts` lista o que tem o `BandId` da banda.
- **Contadores reais no hero**: `UserProfileDto.PostCount` e `BandProfileDto.PostCount` contam só posts publicados. A regra "sem contador falso" (`bandas.md`) continua valendo; agora o número existe.
- Autor desativado (`IsActive = false`) some do feed, das listas e das contagens de comentário.

## Post com vídeo em processamento

- O post pode ser criado enquanto o vídeo ainda converte, como no YouTube. Ele nasce com `PublishedAt = null`.
- **Visibilidade**: post não publicado é visível **só para o autor** (feed, detalhe e listas). Para qualquer outra pessoa ele não existe (`POST_NOT_FOUND`).
- **Publicação**: `IPostRepository.TryPublishAsync` grava `PublishedAt` num `ExecuteUpdateAsync` condicional, `WHERE PublishedAt IS NULL AND todas as mídias Ready`. Quem chama:
  - o job de conversão, depois de marcar o vídeo `Ready` (`PostAppService.ProcessVideo`, evento `POST_PUBLISHED`);
  - o próprio `Create`, logo depois do commit. Assim, se o vídeo ficou pronto entre a validação e o commit, o post não fica preso.
- O `status` do DTO é derivado: `Published` se há `PublishedAt`; senão `Failed` se alguma mídia falhou; senão `Processing`.
- Post não publicado não recebe curtida nem comentário (`POST_NOT_PUBLISHED`, 409). Post de vídeo que falhou fica para o autor apagar.

## Quem pode o quê

| Ação | Quem |
|---|---|
| Ver feed, post, listas, comentários | qualquer pessoa logada (respeitando a visibilidade acima) |
| Criar post | qualquer pessoa; como banda, só integrante |
| Editar | só o autor, **só o texto** (mídia não). Grava `EditedAt` e a tela mostra "editado" |
| Apagar post | o autor, ou um **Owner** da banda se o post é da banda |
| Curtir | qualquer pessoa, em post publicado |
| Comentar | qualquer pessoa, em post publicado |
| Apagar comentário | quem comentou ou o autor do post |

- A checagem fica no `WHERE` (`AuthorUserId == eu`, ou `Band.Members.Any(Owner == eu)`). Quando volta nulo, `FindVisible` separa 404 `POST_NOT_FOUND` de 403 `POST_FORBIDDEN`.
- Apagar post é **soft delete**. Os arquivos das mídias são apagados logo em seguida; o `media:cleanup` refaz e limpa as chaves se algo falhar (`midia-e-video.md`).
- Apagar comentário é soft delete; o comentário some da lista e da contagem.

## Curtida

`PostLike (PostId, UserId)` com chave composta; não herda `EntityBase` (não tem soft delete nem auditoria, é volume alto). `PUT /api/posts/{id}/like` é idempotente: já curtido devolve o mesmo resultado, e a corrida de dois cliques cai na chave primária e é tratada como "já curtido". `DELETE` é `ExecuteDeleteAsync`. Os dois devolvem o `PostDto` atualizado.

## Leitura: projeção num repositório específico

`IPostRepository` (`Infra.Data/Repositories/Posts/`) projeta `PostReadModel` numa ida ao banco: autor, `LikeCount`, `CommentCount`, `LikedByMe`, `HasFailedMedia` e `ViewerOwnsBand`. O genérico não projeta; é o caso de "projeção agregada" do `CLAUDE.md`. Banda (`IBandAppService.SummariesByIds`) e mídia (`PostMedia` + `IMediaUploadAppService.Describe`) são mapeadas em lote por página.

**Exceção registrada à regra "injete o AppService dono":** `PublicProfileAppService` e `BandAppService` leem a contagem de posts direto do `IPostRepository` (`CountPublishedByAuthorAsync`, `CountPublishedByBandAsync`). Injetar `IPostAppService` fecharia um ciclo de DI, porque o `PostAppService` já depende dos dois.

## Post de arquivo

`Post.ArchiveEntryId` (único) é o post automático do mural das antigas (`mural.md`), com o mesmo desenho do post de evento: texto pode ser vazio, `PostDto.archive` (`ArchiveSummaryDto`) é lido ao vivo e em lote pelo `IArchiveRepository`, e apagar o registro apaga o post.

## Post de evento

`Post.EventId` (único) marca o post automático que nasce ao publicar um evento (`eventos.md`). Ele pode ter texto vazio (o `POST_EMPTY` considera o evento), e o `PostDto.event` traz o resumo **ao vivo** do evento, lido em lote por `IEventRepository.ListByIdsAsync`, para o card mostrar "Cancelado" ou "Já rolou" sem mexer no post. Apagar o evento apaga o post.

## Endpoints

Todos autenticados, em `Controllers/`.

| Método e rota | Code de sucesso |
|---|---|
| `GET /api/posts` (paginado) | `POSTS_FOUND` |
| `GET /api/users/{nickname}/posts`, `GET /api/bands/{slug}/posts` | `POSTS_FOUND` |
| `POST /api/posts { content, bandSlug?, mediaIds[] }` | `POST_CREATED` |
| `GET /api/posts/{id}` | `POST_FOUND` |
| `PUT /api/posts/{id} { content }` | `POST_UPDATED` |
| `DELETE /api/posts/{id}` | `POST_DELETED` |
| `PUT` / `DELETE /api/posts/{id}/like` | `POST_LIKED` / `POST_UNLIKED` |
| `GET /api/posts/{id}/comments` (paginado, mais antigos primeiro) | `COMMENTS_FOUND` |
| `POST /api/posts/{id}/comments { text }` | `COMMENT_CREATED` |
| `DELETE /api/comments/{id}` | `COMMENT_DELETED` |

**Validação:**

| Regra | Code |
|---|---|
| texto até 5000 (trim) | `INVALID_POST_CONTENT` |
| sem texto e sem mídia | `POST_EMPTY` |
| mais de 10 fotos (ou mais de 11 ids) | `TOO_MANY_PHOTOS` |
| mais de 1 vídeo | `TOO_MANY_VIDEOS` |
| mídia que não é minha, já usada, vencida, que falhou ou não existe | `MEDIA_NOT_ATTACHABLE` (409) |
| comentário de 1 a 2000 | `INVALID_COMMENT_TEXT` |

**Outros erros:** `POST_NOT_FOUND`, `POST_FORBIDDEN`, `POST_NOT_PUBLISHED`, `NOT_BAND_MEMBER`, `BAND_NOT_FOUND`, `PROFILE_NOT_FOUND`, `COMMENT_NOT_FOUND`, `COMMENT_FORBIDDEN`.

A mídia anexada é a ordem dos `mediaIds`. O índice único em `PostMedia.MediaUploadId` garante que uma mídia só entra em um post, mesmo na corrida.

## Limites

| Política | Limite | Onde |
|---|---|---|
| `post-create` | 30/h por conta | `POST /api/posts` |
| `comment-create` | 60/h por conta | `POST /api/posts/{id}/comments` |

Curtir fica no limite global (240/min): é barato e idempotente.

## Eventos de log (contexto `Posts`)

| Evento | Nível | Dados |
|---|---|---|
| `POST_CREATED` | Info | `PostId`, `BandId`, `Media`, `Published` |
| `POST_PUBLISHED` | Info | `PostId`, `MediaId` (publicado pelo job) |
| `POST_UPDATED` | Info | `PostId` |
| `POST_DELETED` | Info | `PostId`, `BandId`, `ByBandOwner` |
| `COMMENT_DELETED` | Info | `CommentId`, `PostId`, `ByPostAuthor` |

Curtir e comentar não logam: são o caminho feliz de volume alto, e o dado está no banco.

## Roteiro de verificação

Com o Docker, o ffmpeg e a API no ar; conta A (admin, Owner de uma banda), conta B (integrante Member) e uma conta C nova confirmada pelo Mailpit. O script `feed-api.mjs` vive fora do repositório (scratchpad da sessão que criou o módulo); ele cobre os passos abaixo e os de `midia-e-video.md`.

1. **Post de banda em processamento.** A sobe uma foto e um vídeo, cria o post como a banda → `POST_CREATED` com `status: 1`. C tenta postar como a banda → 403 `NOT_BAND_MEMBER`. Sem texto e sem mídia → `POST_EMPTY`.
2. **Visibilidade.** O feed de C não traz o post; o de A traz com `status: 1`. C abre o post → 404. A curte → 409 `POST_NOT_PUBLISHED`.
3. **Publicação.** Quando o vídeo fica `Ready`, `POST_PUBLISHED` sai no log e C abre o post com `status: 0` e as duas mídias.
4. **Limites.** Dois vídeos → `TOO_MANY_VIDEOS`; 11 fotos → `TOO_MANY_PHOTOS`; mídia já usada, de outra pessoa ou que falhou → `MEDIA_NOT_ATTACHABLE`.
5. **Curtidas.** C curte duas vezes → `likeCount: 1`; descurte duas vezes → 0.
6. **Comentários.** C comenta; A lista e vê `canDelete: true` no comentário de C; C apaga o de A → 403; A apaga o de C → `COMMENT_DELETED`; de novo → 404.
7. **Edição.** C edita o post de A → 403; A edita → `editedAt` preenchido.
8. **Listas.** `/api/bands/{slug}/posts` traz só os posts da banda; `postCount` da banda e do perfil batem com as listas; nickname inexistente → `PROFILE_NOT_FOUND`.
9. **Apagar.** B (Member) apaga o post de A → 403; A (Owner) apaga o post de banda feito por B → `POST_DELETED` com `ByBandOwner=true`, e a URL da foto passa a responder 404.
