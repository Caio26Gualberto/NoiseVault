---
paths:
  - "**/NoiseVault.Application/Mentions/**"
  - "**/NoiseVault.Application/PhotoTags/**"
  - "**/NoiseVault.Infra.Data/Repositories/PhotoTags/**"
  - "**/NoiseVault.Api/Controllers/PhotoTagsController.cs"
  - "**/NoiseVault.Domain/Entities/PhotoTag.cs"
---

# Decisões — @menções e marcação de pessoas nas fotos

Item 3 do `IDEIAS.md` ("quem é esse?"), ampliado pelo usuário em 14/10/2026 para ficar "igual Facebook":
- **@menção** de pessoas e bandas no texto do post e nos comentários de post, de evento e do mural;
- **marcação de pessoas nas fotos** do feed e do mural, no ponto da foto em que a pessoa aparece.

É o que mais traz gente das antigas de volta: "você foi marcado numa foto de 1991".

## @menções

### O texto guarda `@handle`, o servidor resolve
- **Não existe tabela de menção.** Uma tabela com `SourceKind` + `SourceId` sem FK seria o polimorfismo que `dominio.md` proíbe, e cada tipo de comentário teria a sua.
- O texto continua texto puro, com `@apelido` (pessoa) ou `@slug` (banda).
- Na **escrita**, o servidor extrai os handles (`MentionRules.Handles`), resolve em lote e notifica.
- Na **leitura**, resolve de novo em lote por página (`IMentionAppService.Resolve`) e devolve `mentions: [{ handle, kind, id, name, avatarUrl }]` no DTO. `kind` é `EMentionKind`: `Person` 0, `Band` 1. O front troca cada `@handle` resolvido por um link com o nome; handle que não existe fica como texto.
- Apelido e slug são **imutáveis** (`auth-e-contas.md`, `bandas.md`), então a menção não quebra com o tempo. Pessoa desativada deixa de ser resolvida e vira texto.
- **Token**: `(?<![a-z0-9._@-])@([a-z0-9][a-z0-9._-]{1,58}[a-z0-9])`, sem distinguir maiúscula de minúscula. Cobre o apelido (com `.` e `_`) e o slug (com `-`). O caractere antes do `@` não pode ser letra, número nem `.`, para um e-mail no texto não virar menção.
- **Colisão**: o mesmo handle pode ser apelido e slug. Nesse caso **vale a pessoa**. O autocomplete do front insere o handle certo, e a colisão só aparece quando apelido e slug são iguais.

### Onde vale e quem é notificado
| Onde | Notificação | Não notifica |
|---|---|---|
| Post: criar | `MentionedInPost` 23 (pessoa), `BandMentionedInPost` 25 (Owners da banda) | quem escreveu |
| Post: editar | só os handles que **entraram** (o conjunto antigo é comparado com o novo) | quem já tinha sido mencionado |
| Comentário de post | `MentionedInComment` 24 / `BandMentionedInComment` 26, com `PostId` | o autor do post, que já recebe `PostCommented` |
| Comentário de evento | 24 / 26, com `EventId` | o organizador, que já recebe `EventCommented` |
| Comentário do mural | 24 / 26, com `ArchiveEntryId` | o autor do registro e quem marcou "Eu estava lá", que já recebem a deles |

- A banda mencionada notifica os **Owners** (`IBandAppService.MemberUserIds(…, Owner)`), com `BandId` na notificação. Quem já foi notificado como pessoa naquele texto não recebe de novo pela banda.
- Nenhuma menção agrupa: cada uma é um aviso.
- Post ainda não publicado (vídeo convertendo): a notificação nasce, mas o `VisibleFor` das notificações já esconde post sem `PublishedAt` até a publicação.
- **Fora**: a descrição de evento e a história do registro do mural não resolvem menção.

### Limite
- Até **10 handles distintos** por texto, resolvidos ou não (`TOO_MANY_MENTIONS`, 400). É contra spam de notificação, e a checagem é só de texto, sem ida ao banco.
- A busca do autocomplete reaproveita `GET /api/users/search` e `GET /api/bands/search` (`user-search`, 60/min).

## Marcação nas fotos

### Modelo
- **`PhotoTag`** (`EntityBase`, soft delete):
  - `MediaUploadId`, `X` e `Y` (`double`, de 0 a 1, relativos à imagem, com 4 casas);
  - `TaggedUserId?` **ou** `Name?` (2–80), nunca os dois (`PHOTO_TAG_TARGET_REQUIRED`). Decisão do usuário: dá para marcar quem não tem conta, só com o nome, sem link e sem notificação;
  - `TaggedByUserId`.
- **A chave é `MediaUploadId`, não `ArchiveMedia.Id`**: o `UpdateAsync` do mural apaga e recria as linhas de `ArchiveMedia` a cada edição, e as marcações se perderiam. O `MediaUploadId` sobrevive à edição.
- **FK `Cascade` em `MediaUploadId`**, a exceção registrada ao padrão `Restrict`. A marcação não tem sentido sem a foto. Quando a mídia sai do registro, o `Discard` apaga a linha do `MediaUpload` de verdade, e o banco leva as marcações junto. Com `Restrict`, esse `DELETE` quebraria.
- **Girar a foto do mural gira as marcações** (`RotateForMedia`, `mural.md`): 90° horário leva `(x, y)` a `(1 − y, x)`, 180° a `(1 − x, 1 − y)` e 270° a `(y, 1 − x)`, com 4 casas. Sem isso, o ponto ficaria no lugar errado da foto.
- Índices: `(MediaUploadId)` e o único filtrado `(MediaUploadId, TaggedUserId)` (não nulo e não apagado): a mesma pessoa não aparece duas vezes na foto (`PHOTO_TAG_DUPLICATE`, checado antes; o índice segura a corrida).
- Até **30** marcações por foto (`TOO_MANY_PHOTO_TAGS`).
- **O que pode ser marcado**: foto (`Kind = Photo`, `Ready`) anexada a **post publicado** ou a **registro publicado** do mural. Foto de suporte, solta, de post em processamento ou de envio pendente → `PHOTO_NOT_TAGGABLE` (409). O alvo é lido pela navegação `MediaUpload.Attachment.Post` / `ArchiveAttachment.ArchiveEntry`.

### Quem pode
| Ação | Quem |
|---|---|
| Ver as marcações | quem vê a foto (vêm no `MediaDto.tags` do post e do registro) |
| Marcar, com conta ou só com o nome | qualquer pessoa logada (decisão do usuário) |
| Remover | quem foi marcado, quem marcou, o autor do post ou do registro, e `Archive.Edit` nas fotos do mural |

- `canRemove` em cada marcação sai da mesma regra; a tela usa só para UX.
- **Remover a própria marcação** é a resposta ao "não quero meu nome aqui". O pedido de remoção da foto inteira (direito de imagem) é outro passo, registrado no `IDEIAS.md`.

### Endpoints
| Método e rota | Code |
|---|---|
| `POST /api/media/{mediaId}/tags { x, y, nickname?, name? }` (`photo-tag`, 60/h) | `PHOTO_TAG_CREATED`, devolve as marcações da foto |
| `DELETE /api/photo-tags/{id}` | `PHOTO_TAG_DELETED` |

**Erros:**
- Validação: `INVALID_PHOTO_TAG_POSITION`, `INVALID_PHOTO_TAG_NAME`, `PHOTO_TAG_TARGET_REQUIRED`.
- Estado: `PHOTO_NOT_TAGGABLE`, `PHOTO_TAG_DUPLICATE`, `TOO_MANY_PHOTO_TAGS`.
- Busca e permissão: `PHOTO_TAG_NOT_FOUND`, `PHOTO_TAG_FORBIDDEN`; e `PROFILE_NOT_FOUND` (apelido inexistente).

### Leitura e notificação
- `MediaDto.tags: [{ id, x, y, user?: ProfileSummaryDto, name?, canRemove }]` é preenchido em lote (`IPhotoTagAppService.ForMedia`) no `PostAppService.MapAsync` e no `ArchiveAppService.MapDetailAsync`. Em qualquer outro lugar (composer, suporte, lista do mural), vai vazio.
- **Ciclo de DI evitado**: o `PhotoTagAppService` descobre o dono da foto pelo `IPhotoTagRepository`, que lê as navegações da mídia, e não pelo `IPostAppService`/`IArchiveAppService`, que dependem dele para preencher as marcações.
- `PhotoTagged` 27 vai para a pessoa marcada, com o `PostId` ou o `ArchiveEntryId` da foto; não agrupa. Marcar a si mesmo não notifica (o `NotifyMany` tira o ator).

## Eventos de log (contexto `Media`)

| Evento | Nível | Dados |
|---|---|---|
| `PHOTO_TAG_CREATED` | Info | `TagId`, `MediaId`, `PostId`, `ArchiveEntryId`, `WithAccount` (o nome livre não entra: é texto do usuário) |
| `PHOTO_TAG_DELETED` | Info | `TagId`, `MediaId`, `BySelf` |

Menção não loga: o dado está no texto, e a notificação tem a própria trilha.

## Roteiro de verificação

Conta A (admin, Owner de uma banda) e contas C, D e E. O script `mentions-tags-api.mjs` vive fora do repositório.

1. **Menção no post.** C posta com a foto "valeu @D e @banda e @naoexiste".
   - `mentions` traz D (`kind` 0, com o nome) e a banda (`kind` 1); o inexistente fica fora.
   - D recebe a 23, e A (Owner) recebe a 25 com a banda.
   - 11 handles → `TOO_MANY_MENTIONS`.
   - Editar acrescentando @E notifica só E, sem repetir D. O feed traz as 3 menções.
2. **Menção em comentário.**
   - D comenta no post mencionando E e C: E recebe a 24, e C, que é o autor do post, não recebe a 24, só o comentário.
   - Em evento: D recebe a 24; A, que é organizador e Owner, não recebe a 26.
   - No mural: C recebe a 24 com o registro.
3. **Foto.**
   - D marca C → `PHOTO_TAG_CREATED`, e C recebe a 27.
   - Marcar C de novo → `PHOTO_TAG_DUPLICATE`. "Zé do Porão" como nome → aceito, sem `user`.
   - Posição 1.5 → `INVALID_PHOTO_TAG_POSITION`; nome e apelido juntos → `PHOTO_TAG_TARGET_REQUIRED`; foto de suporte → `PHOTO_NOT_TAGGABLE`.
4. **Remoção.**
   - O post traz `media.tags`, com `canRemove: false` para E na marcação de C, e E removendo → `PHOTO_TAG_FORBIDDEN`.
   - C remove a própria marcação e, como autor do post, o nome livre.
5. **Mural.**
   - E marca D, e D recebe a 27 com o registro.
   - Editar o registro (o que recria o `ArchiveMedia`) mantém a marcação, com `canRemove` para a curadoria.
   - Trocar a foto do registro leva a marcação junto (`PHOTO_TAG_NOT_FOUND`).

Resultado em 14/10/2026: as 42 checagens do script passaram.
