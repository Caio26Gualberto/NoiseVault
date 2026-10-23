---
paths:
  - "**/NoiseVault.Application/News/**"
  - "**/NoiseVault.Application/Interfaces/INewsSourceClient.cs"
  - "**/NoiseVault.Domain/Entities/NewsArticle.cs"
  - "**/NoiseVault.Infra.Data/News/**"
  - "**/NoiseVault.Infra.Data/Repositories/News/**"
  - "**/NoiseVault.Infra.Data/Context/Seeding/DevSeed.News.cs"
  - "**/NoiseVault.Workers/**/News/**"
  - "**/NoiseVault.Api/Controllers/NewsController.cs"
  - "**/NoiseVault.Api/Controllers/Admin/NewsAdminController.cs"
  - "**/scripts/coletar-noticias.ps1"
---

# Decisões — Notícias do Whiplash.Net

Pedido do usuário em 21/10/2026: uma página com as notícias do dia de rock e metal, tiradas do [Whiplash.Net](https://whiplash.net), **sem trazer a matéria**. O card mostra a foto, o título, a data e o autor, e o clique abre a matéria no site deles.

## O que se copia, e o que não

- **Só metadados**: título, foto, autor, data e o link. O corpo da matéria nunca é baixado para o banco nem mostrado aqui, porque copiar o texto seria republicar conteúdo deles. Esses metadados são os mesmos que o próprio Whiplash publica para prévia em rede social (JSON-LD `Article` e `og:*`).
- **A foto é servida pela URL deles**, sem cópia no S3. É o mesmo raciocínio do `CoverUrl` do Spotify em `discografia.md`: a imagem continua sob o controle de quem a publicou, e nada nosso redistribui o arquivo. O servidor deles entrega a imagem para qualquer `Referer` (testado em 21/10/2026). O risco aceito é que, se eles passarem a bloquear hotlink, as fotos somem e o card cai no fallback sem imagem. A foto é a grande da matéria (`image[0]` do JSON-LD, uns 700 px), não a miniatura de 140×110 da lista, porque o carrossel do front a mostra grande.
- **A atribuição é obrigatória**: o front mostra "Fonte: Whiplash.Net" com link (`frontend/.claude/rules/noticias.md`).
- **Fica fora do SEO** (`seo.md`): o conteúdo é de terceiros, então não entra no sitemap nem no `/seo/...`. Indexar manchete alheia como se fosse nossa é o que um buscador pune como conteúdo duplicado.

## De onde vem cada campo

- **Lista**: `https://whiplash.net/sitemap_ultimas.xml`, com as URLs dos últimos uns 15 dias (871 em 21/10/2026), da mais nova para a mais antiga. Só entra URL no formato `/materias/news_{n}/{id}-{slug}.html` ou `/materias/news_{n}/{id}.html`. O slug é opcional: em 23/10/2026, 50 das 953 URLs do sitemap vinham sem ele, e eram matérias normais, com JSON-LD e `canonical` sem slug. Até essa data a regex exigia o slug e essas notícias eram descartadas em silêncio. Resenhas (`/materias/cds/`) e outras seções continuam fora. O `{id}` é crescente e vira `ExternalId`. O `robots.txt` deles não bloqueia `/materias/` nem os sitemaps. A página `/ultimas.html` tem os mesmos itens, mas pesa 300 KB e não traz o autor.
- **Matéria**: cada URL é baixada uma vez. Do JSON-LD saem `headline`, `author.name`, `datePublished` e `image[0]`. Se o JSON não fizer parse, o título e a foto saem de `og:title`/`og:image` e a data, de uma regex no `datePublished`. Os textos chegam com entidades HTML (`&#34;`) e passam por `WebUtility.HtmlDecode` antes de gravar. Matéria sem título é pulada.
- 🔴 **O `datePublished` deles diz `+00:00`, mas é horário de São Paulo.** Às 01:02 UTC de 22/10/2026, a matéria mais nova estava marcada como `21:43+00:00`: era 21:43 em SP, 19 minutos antes, e não 3 horas antes. Nas amostras dos 15 dias, as publicações vão das 7h às 23h. Por isso o cliente descarta o fuso e converte o relógio com `AppTime.ToUtc`. Se um dia eles corrigirem o fuso, os horários ficam 3 h adiantados e a correção é ler o offset de novo.

## Cloudflare: User-Agent de navegador, HTTP/1.1, e o Windows bloqueado

O Whiplash fica atrás do Cloudflare. Testado em 21/10/2026:

| Cliente | Resultado |
|---|---|
| curl (Windows, SChannel), com qualquer User-Agent não vazio, inclusive `NoiseVaultBot/1.0` | 200 |
| curl sem User-Agent | 403 |
| .NET no **Linux** (imagem `aspnet:10.0`), User-Agent de navegador, HTTP/1.1 | **200** |
| .NET no Linux, User-Agent de robô ou HTTP/2 | 403 `cf-mitigated: challenge` |
| .NET no **Windows**, qualquer combinação (User-Agent, HTTP/1.1 ou 2, TLS 1.2 ou 1.3, ALPN) | 403 `cf-mitigated: challenge` |

- O cliente manda o User-Agent de navegador (`News:UserAgent`, com um padrão no código) e deixa o `HttpClient` no HTTP/1.1, que é o padrão dele. Produção roda em Linux, no container da API (`hospedagem.md`).
- **Em dev no Windows a coleta é sempre bloqueada**: o job registra `NEWS_SOURCE_BLOCKED` e não faz mais nada. Para a página ter conteúdo, o `DevSeed` grava seis manchetes reais coletadas em 21/10/2026, com as datas relativas ao boot (`seed-de-desenvolvimento.md`). Para testar a coleta de verdade, rode a API num container Linux.
- 403, 429 e a página de desafio (`Just a moment...` no corpo) viram `ENewsSourceFailure.Blocked`. O job para na primeira, porque insistir só piora a nota do IP no Cloudflare. A hora seguinte tenta de novo.
- O risco aceito: o Cloudflare pode passar a desafiar o IP do Lightsail (de datacenter) a qualquer momento. Aí o log mostra `NEWS_SOURCE_BLOCKED` toda hora e a página fica com o que já havia sido coletado, até sumir com a retenção. A saída, se acontecer, é pedir ao Whiplash um feed ou a liberação do nosso IP, não tentar burlar o desafio.
- `News:Enabled = false` desliga a coleta: o boot avisa no console e o job sai sem chamar nada. O resto (a lista, a página) continua funcionando com o que estiver no banco.
- 🔴 **O risco aconteceu em 22/10/2026.** A primeira coleta manual no Lightsail (dia 21, sitemap + 40 matérias) passou. Desde a execução seguinte, toda hora volta `NEWS_SOURCE_BLOCKED` com 403 já no sitemap. Um `curl` simples do próprio servidor também recebe `403 cf-mitigated: challenge`, então o bloqueio é do IP, não do .NET. Do IP residencial do dono do projeto, o mesmo pedido dá 200. O Whiplash **autorizou o uso** das manchetes, mas o site não tem ninguém que mexa no Cloudflare deles, então liberar o IP não é uma opção. A saída adotada é a coleta pela máquina do dono do projeto, logo abaixo. Em produção o job fica com `News:Enabled = false`.

## Coleta local: o script manda, a API decide

`api/scripts/coletar-noticias.ps1` roda no Windows do dono do projeto, de hora em hora pelo Agendador de Tarefas, e só quando o PC está ligado. Ele baixa as páginas do Whiplash com o `curl.exe` (o `curl` do Windows passa pelo Cloudflare, o .NET do Windows não, como mostra a tabela acima) e **entrega o conteúdo cru à API**. A API faz o parse com o mesmo código do `WhiplashNewsClient` e grava.

- **Por que pela API e não direto no banco**: o Postgres do Lightsail não tem porta aberta para fora, e assim continua. O script não guarda senha de banco, não duplica o parse, o dedupe nem a regra dos 30 dias. A regra mora num lugar só.
- **Por que o script manda o HTML, em vez de fazer o parse**: assim o script não precisa de .NET nem de Docker, e o parse é o mesmo do job. O HTML é lido em memória pelo `ParseArticle` e descartado. Do corpo da matéria, nada vai para o banco, como na coleta do job.
- **Sem limite de quantidade por execução.** O limite de 40 do job existia para o IP do servidor não tomar bloqueio. No IP residencial ficam a pausa (1,5 s entre matérias, `-PausaSegundos`) e a parada no primeiro 403, 429 ou desafio. A primeira execução enche o banco com o sitemap todo (uns 15 dias), em uns 20 minutos. Depois, cada hora baixa só as novas.
- **Quem sabe o que já temos é o banco**, não um arquivo do script. O script manda o sitemap, a API devolve só o que falta, e o script baixa só isso do Whiplash. Uma lista local poderia divergir do banco depois de uma restauração de backup ou da retenção.
- **PC desligado à noite**: a primeira execução da manhã recupera tudo o que saiu de madrugada, porque o sitemap tem uns 15 dias. A data de cada notícia vem da própria matéria, então ela aparece no horário certo.
- **Login**: o script entra com e-mail e senha de uma conta com `News.Import` (`POST /api/auth/login`). O access token dura 15 min; num 401 o script entra de novo uma vez. A credencial fica em `%LOCALAPPDATA%\NoiseVault\noticias-credencial.xml`, cifrada pelo DPAPI do Windows (`Export-Clixml`), legível só pelo mesmo usuário na mesma máquina. Nunca no Git nem em argumento de linha de comando.
- **A tela `/admin/jobs` não mostra a coleta local.** Ela mostra o que o Hangfire roda, e com a coleta desligada o `news:collect` aparece com `"Disabled": true`. O registro da coleta local fica no log da API (`NEWS_IMPORT_PENDING`, `NEWS_IMPORTED`) e no `%LOCALAPPDATA%\NoiseVault\noticias.log` da máquina.

### Endpoints (`Controllers/Admin/NewsAdminController.cs`, permissão `News.Import`)

| Rota | Entrada | Saída |
|---|---|---|
| `POST /api/admin/news/import/pending` | `{ sitemap }` (o XML cru) | `NEWS_IMPORT_PENDING` com `{ links, pending: [{ externalId, url }] }`, o que falta, do mais novo para o mais antigo. Sitemap sem nenhum link de matéria (o desafio do Cloudflare, por exemplo) → `INVALID_NEWS_SITEMAP` (400) |
| `POST /api/admin/news/import` | `{ url, html }` | `NEWS_IMPORTED` com `{ added, alreadyExists, tooOld }`. URL fora do formato `/materias/news_{n}/{id}-{slug}.html` → `INVALID_NEWS_URL` (400). HTML sem título → `NEWS_ARTICLE_UNREADABLE` (400) |

- O desfecho de um import que deu certo vem como bool no DTO, e não como code novo: notícia que já existe ou que passou dos 30 dias não é erro, é o caminho normal de uma reexecução.
- Corpo até 4 MB (`RequestSizeLimit`). Uma matéria tem uns 150 KB e o sitemap, uns 100 KB.
- Sem rate limit próprio: só uma conta com `News.Import` chega aqui, e o limite global de 240 por minuto cobre um erro no script. `News.Import` não é concedida a nenhum papel por padrão. O `Admin` já tem tudo.

## Modelo

- **`NewsArticle`** (`EntityBase`): `Source` (`ENewsSource`: `Whiplash` = 0), `ExternalId` (long), `Title` (até 300), `Url` (até 500), `ImageUrl?` (até 500, só http(s)), `Author?` (até 120), `PublishedAt` (UTC).
- Índice único em `(Source, ExternalId)` **sem filtro de soft delete**, de propósito. O dedupe da coleta também olha as linhas apagadas, então uma notícia escondida por soft delete não volta na hora seguinte. Nada no código faz soft delete hoje; a retenção apaga de verdade.
- Índice em `PublishedAt`, que é o filtro e a ordem da lista.
- `ENewsSource` existe para o dia em que entrar uma segunda fonte. A interface do cliente é `INewsSourceClient`, a implementação é `WhiplashNewsClient` (`Infra.Data/News/`), um typed `HttpClient` com timeout de 15 s.

## Coleta (`news:collect`, de hora em hora aos 15 min) e retenção (`news:purge`, às 4 h)

`INewsAppService.Collect()`:

1. Lê o sitemap. Se falhar, registra `NEWS_SOURCE_BLOCKED` ou `NEWS_SOURCE_UNAVAILABLE` e sai.
2. Separa os `ExternalId` que ainda não estão no banco e fica com **os 40 mais novos** (`NewsRules.MaxArticlesPerRun`). Na primeira execução entram as 40 mais recentes, e as horas seguintes completam as mais antigas do sitemap, 40 por vez, até chegar aos 30 dias. Notícia nova sempre passa na frente.
3. Baixa uma matéria por vez, com 1 s de pausa entre elas (no máximo 40 requisições por hora, mais o sitemap).
4. Uma matéria que falha (`Unavailable`) é registrada com `NEWS_ARTICLE_FAILED` e a coleta segue para a próxima. Um bloqueio (`Blocked`) encerra a execução.
5. Pula o que tiver mais de 30 dias. Fecha com `NEWS_COLLECTED { Added, Candidates }`.

O `Collect()` devolve `NewsCollectResultDto { added, candidates, failed, blocked, unavailable, disabled }`, e o Trigger o repassa ao Hangfire. É isso que a tela de jobs (`/admin/jobs`, `jobs-hangfire.md`) mostra como resultado: o job termina "Succeeded" mesmo bloqueado, e só o resultado diz que nada entrou. Na mesma tela, o "Rodar agora" do `news:collect` é o jeito de buscar uma notícia urgente fora de hora e de fazer a primeira coleta depois do deploy (`DEPLOY.md`).

`Purge()` apaga de verdade (`ExecuteDeleteAsync`) as notícias com `PublishedAt` de mais de 30 dias (`NewsRules.RetentionDays`). Pedido do usuário: a página é de notícia do dia, não um arquivo.

Os dois jobs rodam na fila `default`. A política de retry está em `jobs-hangfire.md`.

## Endpoint

`GET /api/news?scope=today|earlier&page=&pageSize=` (`[AllowAnonymous]`, paginado) → `NEWS_FOUND` com `PagedResultDto<NewsArticleDto { id, source, title, url, imageUrl, author, publishedAt }>`.

- `scope=today` (o padrão) traz o que foi publicado desde a meia-noite de São Paulo (`AppTime.TodayStartUtc`). `earlier` traz o que é de antes. As duas listas vêm em `PublishedAt` decrescente, com desempate por `Id`. Por isso o front faz duas chamadas, uma para o carrossel e outra para a lista por dia, em vez de separar uma lista só no navegador.
- O `scope` é texto, como o da agenda (`eventos.md`). Qualquer outro valor → `INVALID_NEWS_SCOPE` (400).
- Não tem rate limit próprio: é leitura pública e barata, coberta pelo limite anônimo global (`abuso-e-limites.md`).

## Eventos de log (contexto `News`)

| Evento | Nível | Dados |
|---|---|---|
| `NEWS_COLLECTED` | Info | `Added`, `Candidates` |
| `NEWS_SOURCE_BLOCKED` | Warning | `Added` (até o bloqueio), `Message` |
| `NEWS_SOURCE_UNAVAILABLE` | Warning | `Failure`, `Message` (o sitemap não respondeu ou não fez parse) |
| `NEWS_ARTICLE_FAILED` | Warning | `ExternalId`, `Failure`, `Message` |
| `NEWS_PURGED` | Info | `Removed`, `RetentionDays` |
| `NEWS_IMPORT_PENDING` | Info | `Links`, `Pending` (o script local mandou o sitemap) |
| `NEWS_IMPORTED` | Info | `ExternalId` (só quando a notícia entrou) |

## Roteiro de verificação

1. **Coleta em Linux** (é como produção roda): a API num container, ou um programa de teste que use o `WhiplashNewsClient` na imagem `mcr.microsoft.com/dotnet/aspnet:10.0`. `ListRecentAsync` traz centenas de links. `GetArticleAsync` traz título com aspas já decodificadas, autor, foto `imagens_promo_22/...jpg` e um `PublishedAt` cuja hora local bate com a hora do site.
2. **Job** (`/hangfire` → Recurring jobs → `news:collect` → Trigger now) num ambiente Linux → `NEWS_COLLECTED` com `Added` até 40 e as linhas em `NewsArticles`. Disparar de novo pega as 40 seguintes, mais antigas. Quando não sobra nada, `Added = 0`.
3. **Em Windows**, o mesmo disparo → `NEWS_SOURCE_BLOCKED`, sem linhas novas e sem exceção no Hangfire.
4. **Banco vazio em dev** → `DEV_SEED_APPLIED` com `NewsCreated = 6`. Num segundo boot, `NewsCreated = 0`.
5. `GET /api/news` sem token → 200 `NEWS_FOUND` com as notícias de hoje. `?scope=earlier&page=1&pageSize=10` traz as de antes. `?scope=x` → 400 `INVALID_NEWS_SCOPE`.
6. **Purga**: num banco de teste, puxe um `PublishedAt` para 31 dias atrás e dispare `news:purge` → `NEWS_PURGED` com `Removed = 1`.
7. **Coleta local** contra a API de dev: `.\scripts\coletar-noticias.ps1 -Api http://127.0.0.1:5041 -Configurar` pede a credencial (admin de dev) e roda. O log da API mostra `NEWS_IMPORT_PENDING` com `Pending` perto do total do sitemap, menos as seis do seed. As matérias entram uma a uma (`NEWS_IMPORTED`), e a página `/noticias` passa a ter as de hoje. Rodar de novo → `Pending` de 0 a 3, e só essas são baixadas.
8. `POST /api/admin/news/import` com uma URL que não é de matéria → 400 `INVALID_NEWS_URL`. Com `html` = `<html></html>` → 400 `NEWS_ARTICLE_UNREADABLE`. A mesma matéria duas vezes → a segunda volta `added = false`, `alreadyExists = true`. Sem token → 401; com uma conta sem `News.Import` → 403.
