---
paths:
  - "src/pages/feed/**"
  - "src/api/multipart-upload.ts"
  - "src/lib/video-file.ts"
  - "src/components/app-media-grid/**"
---

# Decisões — Feed no frontend

Rules irmãs: `../api/.claude/rules/feed.md` (post, curtida, comentário, quem pode o quê) e `../api/.claude/rules/midia-e-video.md` (fotos, vídeo, conversão). A regra é do backend; a tela só reflete. Codes em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`.

## Telas

| Caminho | O que mostra |
|---|---|
| `/feed` | composer + feed único e cronológico (`GET /api/posts`), rail com próximos eventos e bandas sugeridas |
| `/feed/:id` | o post inteiro e os comentários (mais antigos primeiro, "Carregar mais" de 100 em 100) |
| `/perfil/:nickname` | seção "Publicações" com os posts da pessoa (inclusive os que ela publicou como banda) e o contador no hero |
| `/bandas/:slug` | seção "Publicações" com os posts da banda, depois de Integrantes, e o contador no hero |

- **Sem abas.** "Tudo/Bandas" saiu junto com o mock: sem "seguir", não existe recorte honesto. Decisão do usuário em 13/10/2026.
- Perfil e banda usam `OwnerPosts` (`pages/feed/components/owner-posts.tsx`), dono: feed. A segunda página importa do caminho da primeira, como os services.

## Lista: acumula e troca o item pelo DTO do servidor

`hooks/use-post-list.ts` é o hook das três listas:
- **"Carregar mais" soma as páginas** (o mock substituía a página 1 pela 2, um bug). Itens repetidos entre páginas são descartados por `id`.
- **Publicar** recarrega a partir da página 1.
- **Curtir e editar** trocam só aquele item pelo `PostDto` que a API devolve (`replace`). **Apagar** tira o item depois do `DELETE` com sucesso (`remove`).
  - É a mesma ideia do `replace(dto)` de bandas: o dado é do servidor, não simulado.
  - Refazer a busca de todas as páginas acumuladas a cada curtida seria caro e faria a rolagem pular.
- **Polling do processamento**: a cada 5 s, o hook busca de novo (`GET /api/posts/{id}`) os posts do **próprio autor** que estão em `processing` e troca quando o status muda. O polling para quando não há nenhum, e também com a aba escondida (`usePolling`).

## Composer

- **"Publicar como"**: aparece só se `getMyBands()` não vier vazio. "Você (nome)" ou "Banda (banda)". Depois de publicar, volta para "Você", para o próximo post não sair como banda sem querer.
- **Anexos**: foto (JPEG, PNG, WebP, até 15 MB, várias de uma vez) e vídeo (`video/*`, até 500 MB, até 10 min). Juntos ou separados, **até 10 fotos e 1 vídeo**.
  - A checagem local (`checkImageFile` e `checkVideoFile`) recusa cedo, com o aviso inline em vermelho embaixo dos anexos. A duração vem dos metadados do `<video>`; se o navegador não conseguir ler (`.mkv`, alguns `.mov`), passa e quem decide é o `ffprobe` do servidor.
  - **O envio começa ao escolher o arquivo**, não ao publicar. Foto vai por `POST /api/media/photos` (spinner); vídeo vai em partes direto ao S3 com porcentagem (`uploadVideo` em `media-service.ts`).
  - "Publicar" fica desabilitado e mostra "Enviando…" enquanto algum envio não terminou.
  - Remover um anexo aborta o envio (`AbortController`) ou apaga a mídia já enviada (`DELETE /api/media/{id}`, sem toast). Anexo que falhou fica marcado "Falhou" até a pessoa remover.
  - Mídia enviada e não publicada (aba fechada) não é apagada pela tela: o `media:cleanup` do backend apaga depois de 24 h.
- **Upload multipart** (`src/api/multipart-upload.ts`): é HTTP, então mora em `src/api/`.
  - Usa um axios próprio, **sem** o interceptor de auth (a URL pré-assinada já é a autorização, e um `Authorization` extra invalidaria a assinatura) e **sem** o timeout de 15 s.
  - 4 partes em paralelo, até 3 tentativas por parte com espera crescente.
  - O progresso soma os bytes de todas as partes. O `ETag` vem do header exposto pelo CORS do bucket.

## Post na tela

- **Post de arquivo**: com `post.archive`, o meta mostra "publicou no mural das antigas" e o `ArchiveEmbed` (capa com sépia, selo "Das antigas", data na precisão gravada, contagem de fotos e vídeos) leva a `/mural/:id` (`mural.md`).
- **Post de evento**: quando `post.event` vem preenchido, o card mostra "publicou um evento" no meta e o `EventEmbed` (banner 21:9, pílula de data, título, data e local, "Cancelado"/"Já rolou") com link para `/eventos/:id`. O texto pode ser vazio. Detalhes em `eventos.md`.
- **Cabeçalho**: post de banda mostra o avatar (com selo de verificada) e o nome da banda, e embaixo "por @nickname", tempo relativo e "editado". Post de pessoa fica como antes.
- **Menu "…"**: aparece só com `canEdit` ou `canDelete` vindos da API.
  - "Editar texto" abre `PostEditDialog`: só o texto, sem trocar a mídia, como a API.
  - "Apagar" usa `useAppConfirm`. No detalhe, apagar volta ao feed.
  - O trigger é um botão estilizado, e não um `AppIconButton`, porque o tooltip do `AppIconButton` não aceita ser filho `asChild` do trigger do dropdown.
- **Estados** (`post-status.ts`): `published` mostra curtir e comentar; `processing` mostra o selo "Processando" e a frase de que o post aparece para todo mundo quando ficar pronto; `failed` mostra "Falhou" e a orientação de apagar.
- **Mídia**: `AppMediaGrid` recebe `url`, `posterUrl`, `durationSeconds` e `state`.
  - Vídeo mostra o poster, o play e a duração. Clicar em qualquer mídia abre o `AppMediaViewer` naquela posição, com navegação entre as mídias prontas do post; vídeo toca no `AppVideoPlayer` (`design-system.md`). O visualizador saiu do `AppDialog` porque o botão de fechar do diálogo ficava em cima do vídeo e o hover dele aparecia dentro da imagem.
  - Mídia em processamento ou que falhou vira um placeholder com o texto de `common.json` (`media.*`).
- **Enums próprios**: `post-status.ts`, `media-kind.ts` e `media-status.ts`, cada um num arquivo, nunca no service (`mocks.md`).

## Menções e marcações

Os comentários (e, no feed, o texto do post) usam `AppMentionTextarea` para escrever e `AppRichText` para mostrar; as fotos publicadas aceitam marcação de pessoas no viewer. Tudo em `marcacoes.md`.

## Roteiro de verificação

Com o Docker, o ffmpeg, a API e o Vite no ar; conta A (admin, Owner de uma banda) e conta B confirmada. O script `feed-ui.mjs` (Playwright + Brave) vive fora do repositório.

1. **Publicar com mídia (1440px).** A abre `/feed`, escolhe "Serra Negra (banda)", escreve, anexa 2 fotos e o vídeo de teste de 100 MB.
   - As miniaturas mostram spinner e porcentagem, e "Publicar" diz "Enviando…".
   - Ao terminar, publica e o post aparece no topo com o avatar da banda, "por @admin" e o selo "Processando".
2. **Processamento.** Em segundos o selo some (polling) e o vídeo mostra o poster e "0:40". A engrenagem do player lista as resoluções geradas (1080p, 720p, 480p, 360p para um original 1080p) e "Automática"; escolher 360p troca a imagem na hora. Clicar abre o visualizador em tela cheia ("1 de 3", fechar no canto da tela) e o vídeo toca no player da marca: barra vermelha com prévia do tempo no hover, controles que somem durante a reprodução, `k` pausa e o play grande volta ao centro.
3. **Editar.** "…" → "Editar texto" → salvar: toast "Publicação editada." e "editado" no cabeçalho.
4. **Outra conta (390px).** B vê o post da banda no feed, curte (coração cheio, "1"), abre o detalhe e comenta: "1 comentário" e a lixeira no próprio comentário.
5. **Perfil e banda.** `/bandas/serra-negra` mostra "N publicações" no hero e a seção "Publicações" com os posts da banda; `/perfil/admin`, o contador e a lista.
6. **Recusas locais.** Uma 11ª foto ou um segundo vídeo mostram o aviso de limite; um `.txt` no botão de foto mostra "Use JPEG, PNG ou WebP.".
