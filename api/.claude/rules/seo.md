---
paths:
  - "**/NoiseVault.Application/Seo/**"
  - "**/NoiseVault.Api/Seo/**"
  - "**/NoiseVault.Api/Controllers/Seo/**"
  - "infra/cloudfront/**"
---

# Decisões — SEO: leitura sem login e HTML para buscadores

Item 16 do `IDEIAS.md`, começado em 20/10/2026. Até aqui o Google não via nada: todo endpoint de conteúdo herdava o `[Authorize]` do `ApiBaseController`, e o front inteiro ficava atrás do `RequireAuth`. Só o login e a privacidade eram indexáveis.

Decisões do usuário em 20/10/2026:
- ficam **abertas para leitura sem login** a banda, o evento com a agenda e o mural com a linha do tempo. **Perfil de pessoa continua fechado**, assim como o feed;
- os **robôs das IAs ficam liberados** (ser citado no ChatGPT, no Gemini, no Perplexity vale mais do que proteger o texto público);
- quem chega pela busca vê **a página de verdade**, no mesmo layout, só leitura (`frontend/.claude/rules/visitante.md`). É o que o Google exige: o robô e a pessoa veem o mesmo conteúdo.

## Leitura sem login

`[AllowAnonymous]` **só nas actions GET** abaixo; tudo que escreve continua com login. Com token, a mesma rota continua sabendo quem lê (`UserIdOrNull`).

| Endpoint | Sem login devolve |
|---|---|
| `GET /api/bands/{slug}` | o perfil da banda, com `myRole` e `followedByMe` nulos/falsos |
| `GET /api/bands/{slug}/discography` | a discografia, sem `canManage` |
| `GET /api/bands/{slug}/events` · `GET /api/bands/{slug}/archive` | os eventos e os registros da banda |
| `GET /api/events` · `/api/events/upcoming` · `/api/events/{id}` | a agenda e o evento; `interested` e `canManage` falsos, e o filtro `interested=true` volta vazio |
| `GET /api/archive` · `/api/archive/years` · `/api/archive/{id}` | o mural, a linha do tempo e o registro publicado; `isMine`, `canEdit`, `canDelete`, `canReview` falsos |
| `GET /api/cities` · `/api/cities/featured` | o filtro de cidade da agenda e do mural |

- **Como**: os AppServices leem quem é o leitor por `UserIdOrNull ?? 0`. Os repositórios já recebiam `viewerUserId` e só o usam para `LikedByMe`, `AttendedByMe`, `Interested`, "só os que vou" e `ViewerOwnsBand`; o id 0 nunca casa com ninguém, o mesmo truque do `SharePreviewAppService` (`compartilhamento.md`). O `BandAppService.FindAccess` devolve papel nulo sem login, e o `ArchiveAppService.PermissionsAsync` devolve tudo falso sem consultar o `IPermissionChecker`.
- **O pendente do mural continua invisível**: sem login, `FindReadable` só devolve o registro `Published`.
- **O que some sem login, por privacidade** (decisão registrada no plano aprovado em 20/10/2026): no registro do mural, as **marcações de pessoas nas fotos** (`media[].tags` vazio) e a **prévia de "quem estava lá"** (`attendees` vazio). Ficam só as contagens. A regra é da API, não da tela: o front não tem como esconder o que a API entregou.
- **Continuam com login**: comentários de evento e do mural, `attendees`, vagas da banda (`ensaio.md`), posts da banda e do feed, perfis de pessoa, bandas sugeridas e a busca de bandas.
- **Limite**: sem login, o limiter global já particiona por IP (`ApiRateLimiting.PartitionKey`), 240/min. Nada novo para a SPA.

## HTML para robô

O robô de busca e o de IA não executam JavaScript com confiança (a maioria dos de IA não executa nada), e a SPA devolve o mesmo `index.html` vazio para toda rota. A função da borda (`infra/cloudfront/web-viewer-request.js`, `hospedagem.md`) manda esses robôs para a API, que devolve o HTML com o conteúdo:

| Página | Rota na API | O que vai |
|---|---|---|
| `/` e `/eventos` | `GET /seo` · `GET /seo/eventos` | a agenda: próximos eventos, com links |
| `/eventos/{id}` | `GET /seo/eventos/{id}` | título, data e hora, local, cidade, line-up (com link para a banda cadastrada), descrição inteira, ingresso e preço, cancelado |
| `/mural` | `GET /seo/mural` | os registros mais recentes, com links |
| `/mural/{id}` | `GET /seo/mural/{id}` | título, data na precisão do registro, local, cidade, bandas, a história inteira e as imagens com legenda (sem marcações) |
| `/bandas/{slug}` | `GET /seo/bandas/{slug}` | nome, cidade, ano, gêneros, descrição, integrantes (nome e papel, sem link), discografia, próximos shows e registros do mural |
| `/sitemap.xml` | `GET /seo/sitemap.xml` | todas as bandas, eventos e registros publicados, com `lastmod` |

- **O HTML** (`Api/Seo/SeoPage.cs`): `<title>`, `description`, `canonical` com a URL da página em `noisevault.net`, `og:*`, JSON-LD e o conteúdo em HTML semântico (`h1`, `time`, listas), com links entre as páginas. **Sem redirect**, ao contrário do `/s/...`: o robô precisa ficar e ler.
- **JSON-LD** (schema.org): `MusicEvent` (`startDate`, `endDate`, `eventStatus` cancelado, `location` com `Place` e `PostalAddress`, `performer` com `MusicGroup`, `offers` com o link do ingresso), `MusicGroup` (`foundingDate`, `location`, `genre`, `sameAs` com as redes), `CreativeWork` para o registro do mural (`dateCreated` na precisão, `contentLocation`, `about` as bandas) e `WebSite` na agenda.
- **Os mesmos dados da SPA**: quem lê o HTML do robô vê o que o visitante vê na página. Não é cloaking. Por isso valem as mesmas exclusões da leitura sem login.
- **Quem monta** é o `SeoPageAppService` (`Application/Seo/`), que lê pelos AppServices donos (`IBandAppService`, `IEventAppService`, `IArchiveAppService`, `IBandDiscographyAppService`) sem login, já que eles passaram a aceitar o leitor anônimo. Os textos de data e cidade são os do `SharePreviewAppService`, numa classe estática compartilhada em `Application/Share/`.
- **Outra superfície**: `Controllers/Seo/SeoController.cs`, `ControllerBase` próprio, `[AllowAnonymous]`, fora de `/api` e sem envelope, como o `ShareController`.
- **404** (id inexistente, banda apagada, envio pendente): HTML curto com `noindex`.
- **Cache**: `Cache-Control: public, max-age=600`. A borda guarda 10 minutos por URL; o `User-Agent` não entra na chave.
- **Limite**: `seo-page`, 300/min por IP. Pela borda, o IP é o do ponto do CloudFront, e um ponto atende vários robôs; o cache de 10 minutos segura a maior parte.
- **`/s/...` ganha `noindex`** (`compartilhamento.md`): a página de verdade é a canônica, e a do link de compartilhamento não deve competir com ela.

## Robôs

- **Prévia** (vão para `/s/...`): `facebookexternalhit`, `Facebot`, `WhatsApp`, `Twitterbot`, `TelegramBot`, `Discordbot`, `Slackbot`, `LinkedInBot`, `SkypeUriPreview`. Testados primeiro.
- **Busca e IA** (vão para `/seo/...`): `Googlebot`, `bingbot`, `DuckDuckBot`, `YandexBot`, `Applebot`, `GPTBot`, `OAI-SearchBot`, `ChatGPT-User`, `ClaudeBot`, `Claude-SearchBot`, `Claude-User`, `PerplexityBot`, `Perplexity-User`, `CCBot`.
- **`User-Agent` pode ser falsificado**: quem se passa por Googlebot recebe o HTML do conteúdo público, que já é público. Não há o que proteger.
- **`robots.txt`** (`frontend/public/robots.txt`): libera tudo, bloqueia as telas de conta, de suporte, de administração e de auth, e aponta o sitemap. O `Google-Extended` fica liberado.
- **`llms.txt`** (`frontend/public/llms.txt`): o resumo do site para IAs, com links para a agenda, o mural e o sitemap. É uma proposta recente, sem garantia de leitura; custa pouco.

## Roteiro de verificação

Com a API no ar, sem token:
1. `GET /api/events`, `/api/events/{id}`, `/api/archive`, `/api/archive/{id}`, `/api/bands/{slug}`, `/api/bands/{slug}/discography`, `/api/cities` → 200. No registro, `media[].tags` e `attendees` vazios; `canEdit` falso.
2. `GET /api/archive/{id}/comments`, `/api/events/{id}/comments`, `/api/users/{apelido}`, `/api/posts` → 401. `PUT /api/events/{id}/interest` → 401.
3. Um envio pendente em `/api/archive/{id}` → 404.
4. Com token, as mesmas rotas devolvem `interested`, `likedByMe`, `canManage` como antes.
5. `/seo/eventos/{id}`, `/seo/bandas/{slug}`, `/seo/mural/{id}` → HTML com `canonical`, JSON-LD válido (Rich Results Test do Google, em produção) e o conteúdo; `/seo/eventos/999999` → 404 com `noindex`. `/seo/sitemap.xml` → XML com as três seções.
