---
paths:
  - "**/NoiseVault.Application/Share/**"
  - "**/NoiseVault.Api/Share/**"
  - "**/NoiseVault.Api/Controllers/Share/**"
---

# Decisões — Prévia do link ao compartilhar

Ideia 1 do `IDEIAS.md`, feita em 14/10/2026 pelo caminho da **rota de compartilhamento**: o botão "Compartilhar" gera `/s/<página>`, e essa rota devolve um HTML com as tags Open Graph e manda a pessoa para a página de verdade. A prévia no link copiado da barra do navegador (a opção "pelo `User-Agent`") ficou como **1.b**, porque dependia de como o front fosse hospedado. Em 20/10/2026 o usuário decidiu levar o front para S3 + CloudFront, e a função da borda manda o robô que pede `/mural/12` para esta mesma rota, como `/s/mural/12` ("Produção" abaixo, `hospedagem.md`).

## Por que uma rota e não a SPA

- O robô do Facebook, do WhatsApp e do Telegram não executa JavaScript: lê só o HTML da primeira resposta.
- A SPA devolve o mesmo `index.html` para toda rota, sem título nem imagem.
- A rota `/s/...` é a única que responde HTML montado no servidor.

## A rota

- `GET /s/{secao}/{chave}`, em `Controllers/Share/ShareController.cs`. É **outra superfície** (`CLAUDE.md`, "Rotas e escopo"):
  - fora de `/api` e sem o envelope; responde `text/html`;
  - `ControllerBase` próprio, `[AllowAnonymous]`: quem pede é um robô, sem login.
- **O caminho é o da página com `/s` na frente**:

| Link | Página | O que vai na prévia |
|---|---|---|
| `/s/mural/{id}` | `/mural/{id}` | título; data na precisão do registro ("setembro de 1991") · local · cidade · bandas · "N fotos" · "N vídeos" · "N pessoas estavam lá", e o começo da história; capa (foto ou poster do vídeo) |
| `/s/eventos/{id}` | `/eventos/{id}` | título (com "[Cancelado]" quando for); dia e hora na zona do app · local · cidade · "com" o line-up · "N pessoas vão" (some no cancelado), e o começo da descrição; banner |
| `/s/feed/{id}` | `/feed/{id}` | "Fulano (\@apelido) no Noise Vault"; "N fotos · N vídeos · N curtidas · N comentários · 18 de outubro de 2026" e, depois de dois-pontos, o começo do texto entre aspas, com os \@handles trocados pelo nome (sem texto, um convite para ver no Noise Vault); primeira foto (ou o avatar). O post automático de evento e de mural usa a prévia do evento ou do registro |
| `/s/bandas/{slug}` | `/bandas/{slug}` | nome; "Banda · cidade · desde 1991" e o começo da descrição; capa ou avatar |
| `/s/artistas/{slug}` | `/artistas/{slug}` | o mesmo, com "Artista solo · cidade · desde 2012"; `/s/bandas/` de um solo também responde, e o `refresh` leva para `/artistas/` (`bandas.md`) |
| `/s/perfil/{apelido}` | `/perfil/{apelido}` | "Nome (\@apelido)", começo da bio, avatar |

- **A descrição imita a do Instagram** (19/10/2026): a primeira prévia, só com o texto ou "Publicação no feed do Noise Vault", saía sem vida no WhatsApp. Agora abre com uma linha de dados (contagens, data, local, line-up), separados por " · "; contagem zero não aparece, e o singular é "1 curtida". O texto vem depois.
- O texto do conteúdo vai com até 200 caracteres, sem quebra de linha. Sem imagem, vai o ícone do app (`/icons/icon-512.png`).
- **Só conteúdo que está publicado**: registro `Published`, post com `PublishedAt`, evento e banda não apagados, pessoa ativa.
  - Envio pendente do mural, post em processamento, id inexistente ou seção desconhecida → **404** com a prévia genérica do Noise Vault, e o redirecionamento vai para `/feed`.
- **O HTML**:
  - `og:site_name`, `og:locale` (`pt_BR`), `og:type`, `og:title`, `og:description`, `og:image`, `og:url`, `og:image:alt`, `theme-color`, mais as `twitter:*` e a `description`;
  - `<meta http-equiv="refresh">` e `location.replace` para a página. A pessoa nem vê a tela intermediária: cai na SPA, que pede login se precisar;
  - os textos passam por `HtmlEncoder` com todos os alfabetos liberados: acento sai como acento, e `<`, `>`, `&` e aspas são escapados.
- 🔴 **`og:url` é o próprio link `/s/...`, não a página.** O Facebook segue o `og:url` e lê as tags de lá: apontando para a SPA, a prévia sumia.
- `Cache-Control: public, max-age=300`. O robô e a borda guardam por 5 minutos; título editado aparece depois disso (no Facebook, só depois de pedir nova leitura no Sharing Debugger).
- **`<meta name="robots" content="noindex">`** (20/10/2026, `seo.md`): a página de verdade é a canônica, e o `/s/...` não deve competir com ela na busca. O robô de prévia ignora o `noindex`.

## Montagem: `SharePreviewAppService`

- Lê pelos repositórios (`IArchiveRepository.FindAsync`, `IEventRepository.FindAsync`, `IPostRepository.FindVisibleAsync`, e o genérico para banda, usuário e mídia), com o leitor **anônimo** (`viewerUserId = 0`).
- É a exceção registrada à regra "injete o AppService dono": os AppServices de cada módulo usam `ICurrentUser.UserId`, que lança sem login. O mesmo vale para o ponto em que o `CLAUDE.md` permite AppService novo, "quando o consumo é de outra superfície".
- As menções do post são resolvidas pelo `IMentionAppService` (`marcacoes.md`), que não depende de usuário.
- `Build` devolve o DTO cru (ou nulo), e não `Result`: a resposta não é envelope.

## Risco aceito

**Qualquer pessoa vê, sem login, o título, a imagem e o começo do texto** de qualquer conteúdo publicado. Basta montar `/s/feed/{id}` com um id em sequência. É o preço de o robô ler a prévia. O texto vem cortado em 200 caracteres e só sai o que já está publicado na comunidade.

Se virar problema, o link de compartilhamento passa a levar uma assinatura (HMAC de seção + chave, gerada pela API), e a rota recusa link sem assinatura válida.

## Limite

`share-preview`: 120/min por IP. O robô do Facebook pede algumas vezes por link; 120 por minuto segura quem tenta varrer os ids.

## Produção

~~O front no Amplify precisa de um rewrite `/s/<*>` → `https://<api>/s/<*>` (200, proxy), antes do rewrite da SPA.~~ Desde 20/10/2026 quem decide é a função `infra/cloudfront/web-viewer-request.js`, na distribuição do front (`hospedagem.md`, "Front e a prévia de links"):

- `/s/...` vai para a origem da API, como antes;
- robô de prévia (`facebookexternalhit`, `WhatsApp`, `TelegramBot` e afins) em `/{feed,mural,eventos,bandas,artistas,perfil}/{chave}` (fora do `ensaio.`) vai para a mesma origem como `/s/{seção}/{chave}`. A API não muda nada: recebe o mesmo caminho do botão "Compartilhar";
- a pessoa recebe a SPA, como sempre.

O `og:url` continua sendo o link `/s/...`, e o Facebook, que segue o `og:url`, cai na mesma rota. Em dev, o `vite.config.ts` repassa só `^/s/` (sem desvio de robô).

## Roteiro de verificação

Com a API no ar, pelo Vite (`http://localhost:5173/s/...`):
1. `/s/mural/1`, `/s/eventos/1`, `/s/feed/{id}`, `/s/bandas/{slug}` e `/s/perfil/admin` → 200, com `og:title`, `og:description` (data e local em português, acento sem entidade), `og:image` e `og:url` igual ao próprio link. O `refresh` aponta para a página.
2. O post com `@apelido` mostra o nome na descrição.
3. Envio pendente (`/s/mural/{id}`), id inexistente e seção desconhecida → 404 com a prévia genérica e o refresh para `/feed`.
4. `/suporte` continua sendo a SPA: o proxy só pega `/s/`.

Resultado em 14/10/2026: os passos passaram, junto do roteiro da tela (`frontend/.claude/rules/compartilhamento.md`).
