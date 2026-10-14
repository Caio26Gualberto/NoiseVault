---
paths:
  - "src/components/app-mention-textarea/**"
  - "src/components/app-rich-text/**"
  - "src/components/app-media-viewer/**"
  - "src/pages/feed/mention.ts"
  - "src/pages/feed/photo-tags.ts"
  - "src/pages/feed/services/mentions-service.ts"
---

# Decisões — @menções e marcação de pessoas nas fotos no frontend

Rule irmã: `../api/.claude/rules/marcacoes.md`. A regra é do backend; a tela só reflete. Pedido do usuário em 14/10/2026: "igual Facebook", com menção em post e comentários e marcação nas fotos do feed e do mural.

## @menções

- **Escrever**: `components/app-mention-textarea/`.
  - Um textarea que, ao digitar `@` + 2 letras antes do cursor, abre a lista de pessoas e bandas embaixo do campo: avatar, nome, `@handle` e o selo "Banda".
  - ↑/↓ escolhem; Enter e Tab inserem `@handle ` no lugar do termo; Esc fecha. Fora da lista, o Enter funciona como sempre.
  - A busca entra por prop (`search`), porque componente `App*` não importa service de página. O molde é o `layout/people-search.tsx`: debounce de 250 ms, contador contra resposta velha e `onMouseDown preventDefault` nas opções.
- **Buscar**: `pages/feed/services/mentions-service.ts`.
  - `searchMentions` junta `searchPeople` (4) e `searchBands` (3) em paralelo; uma busca que falha não derruba a outra.
  - `searchTaggablePeople` (6 pessoas) serve à marcação.
  - Eventos e mural importam do caminho do feed.
- **Mostrar**: `components/app-rich-text/`.
  - Recebe `text` e `mentions` (`{ handle, name, href }`) e renderiza com `whitespace-pre-line`, trocando cada `@handle` resolvido pelo **nome** como link (`font-medium text-primary`).
  - Handle que a API não resolveu fica como texto. A regex é a mesma da API.
- **Conversão**: `pages/feed/mention.ts` espelha o `EMentionKind` (`person` 0, `band` 1). O `toRichMentions` monta o `href` por `Record` exaustivo: `/perfil/:nickname` ou `/bandas/:slug`.
- **Onde**:
  - o composer do feed, o diálogo de editar post e o `CommentComposer` (o mesmo em post, evento e mural) usam o `AppMentionTextarea`;
  - o texto do post (`post-card.tsx`) e os comentários de `/feed/:id`, `/eventos/:id` e `/mural/:id` usam o `AppRichText`.

## Marcação nas fotos

- **No `AppMediaViewer`** (`photo-tags.tsx`):
  - o `ViewerItem` ganhou `tags` (`AppPhotoTag`: posição, nome, `href` ou nulo, `canRemove`) e `taggable`;
  - o viewer recebe `tagging`: `searchPeople`, `onAdd(itemId, x, y, { nickname } | { name })` e `onRemove`.
- **Ver**:
  - as etiquetas aparecem ao passar o mouse sobre a foto: um ponto `primary` e o nome em `bg-scrim/85`, com link para o perfil quando há conta;
  - no toque, "Mostrar marcações" as deixa fixas;
  - embaixo da foto fica "Com: Fulano, Beltrano", da esquerda para a direita (a API ordena por `X`).
- **Marcar**:
  - "Marcar pessoas" liga o modo, e o cursor vira mira;
  - o clique calcula `x` e `y` sobre o próprio `<img>`, que não tem sobra lateral: o tamanho dele é o da imagem, com `max-w`/`max-h`;
  - no ponto aparece um quadrado e um seletor com "Quem é?", que busca pessoas e oferece "Marcar só com o nome: …" a partir de 2 letras;
  - Esc no seletor fecha só o seletor (`data-dismiss-guard`, como o menu de qualidade do player), e as setas do teclado não trocam de foto enquanto o foco está no campo.
- **Remover**: o × na etiqueta quando `canRemove`, com `useAppConfirm`.
- **Depois de marcar ou remover**, a tela busca o post (`getPost` → `onUpdated`) ou o registro (`getArchiveEntry` → `replace`) de novo: "ação → service → refresh". O toast de sucesso sai do código (`PHOTO_TAG_CREATED`, `PHOTO_TAG_DELETED`).
- **Onde dá para marcar**: só foto pronta de post publicado (`post-card.tsx`) ou de registro publicado do mural (`mural/detalhe.tsx`). O resto do `AppMediaGrid` não passa `tagging`.
- `pages/feed/photo-tags.ts` converte o `PhotoTagDto` (`MediaDto.tags`) para `AppPhotoTag` e monta o `tagging` com `addPhotoTag`/`removePhotoTag` (`media-service.ts`, o dono do `MediaDto`).

## Notificações

- Kinds 23–27 em `notification-kind.ts`/`notification-message.ts`. O link vai para o alvo: post, evento ou registro.
- A frase muda pelo lugar (`withPlace`):
  - "mencionou você num comentário" no post;
  - "… num comentário em **Evento**";
  - "… numa história em **Registro**";
  - "marcou você numa foto em **Registro**".

## Roteiro de verificação

Com a API e o Vite no ar. O script `mentions-tags-ui.mjs` (Playwright + Brave) registra uma pessoa pelo Mailpit.

1. **Composer (1440).**
   - Digitar `@` + o apelido abre a lista com a pessoa, e Enter insere `@apelido `.
   - Publicado com uma foto, o post mostra o nome como link para `/perfil/:apelido`.
2. **Foto.**
   - No viewer, "Marcar pessoas" → clicar na foto → "Quem é?" → escolher: a etiqueta aparece no ponto, e a legenda mostra "Com: …".
   - Marcar "Zé do Porão" só com o nome → "Com: Zé do Porão, Joana …".
   - O × com a confirmação remove.
3. **Comentário.** Em `/feed/:id`, a menção pelo autocomplete vira link no comentário publicado.
4. **Notificações (390).** A pessoa vê no balão do sino "mencionou você num post", "marcou você numa foto" e "mencionou você num comentário".

Resultado em 14/10/2026: as 11 checagens passaram.
