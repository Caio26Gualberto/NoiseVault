# CLAUDE.md — Frontend (web)

## Sobre este projeto

**Noise Vault** — site público da cena do ABC Paulista (metal, punk, hardcore e afins): feed, mural das antigas, eventos, perfis de pessoas e de bandas, notificações. Quem usa é o fã e o músico da região; a administração (moderação, equipe) é a mesma aplicação com permissão. Reescrita do zero do legado "NewLevel" (`C:\Projetos\Pessoal\Alvo\newlevel`), que não serve de referência de código nem de tela.

**Estado da entrega atual:** auth é real (`/entrar` com Google e Facebook, `/entrar/apelido`, `/entrar/vincular`, `/criar-conta`, `/confirmar-email`, `/esqueci-senha`, `/redefinir-senha`, `/definir-senha`, `/bem-vindo`); perfil e mídia também (`/meu-perfil` com avatar, capa e o perfil público, e `/minha-conta` só com o sensível (acesso, senha, notificações, localização), `/perfil/:nickname`, busca de pessoas na topbar — `.claude/rules/perfil.md`); bandas e convites também (`/bandas/:slug`, `/bandas/nova`, `/bandas/:slug/editar` e a seção de convites no painel do sino; o artista solo em `/artistas/:slug`, com "Novo projeto" no perfil e o "Costuma tocar com" entre páginas — `.claude/rules/bandas.md`); o feed também (`/feed`, `/feed/:id`, publicações no perfil e na banda, com fotos e vídeo — `.claude/rules/feed.md`); os eventos também (`/eventos`, detalhe, formulário, seção Shows da banda e o rail do feed — `.claude/rules/eventos.md`). O mural das antigas também (`/mural` e o detalhe, o formulário com permissão, o envio da comunidade em `/mural/enviar` e `/mural/envios`, e a fila da curadoria em `/admin/mural` — `.claude/rules/mural.md`); Vídeos, Fotos e Moderação saíram do produto. O Ensaio também, com moldura própria em `ensaio.noisevault.net` (em dev, `/ensaio`): vitrine "Procura-se banda", ficha de músico, vagas da banda e o "Tenho interesse" (`.claude/rules/ensaio.md`). As notificações também (o balão do sino, como no Facebook, em tempo real por SignalR e a preferência em `/minha-conta` — `.claude/rules/notificacoes.md`). O suporte também (`/reportar-problema` com prints e vídeo, `/suporte` com os relatos e a conversa, e a caixa de entrada `/admin/suporte` com permissão `Support.*` — `.claude/rules/suporte.md`). As notícias também (`/noticias`, pública: manchetes do Whiplash.Net com o link para a matéria — `.claude/rules/noticias.md`). Não sobrou tela mockada: o último `mocks.ts` e o `mock-delay.ts` morreram com o suporte (`.claude/rules/mocks.md`). Institucionais (podcasts, sobre, loja) são páginas estáticas, sem backend; a loja parceira é a Metal Music (Santo André, `pages/institucional/loja.tsx`), com links para o site, o WhatsApp e as categorias da loja.

## Como trabalhar neste repositório

Arquitetura e convenções do frontend. **As telas de referência são `pages/criar-conta`** (formulário com zod, estados de sucesso, service real) **e `pages/eventos`** (listagem paginada com filtros em `hooks/use-eventos-page.ts`, detalhe, formulário em rota própria, tudo sobre a API real). Quando tiver dúvida, copie a estrutura delas.

Este projeto consome a API (`../api` — ver o `CLAUDE.md` de lá). **A interface muda a cada projeto; a arquitetura não.** O esqueleto veio do boilerplate em `C:\Users\Administrador\Desktop\Boilerplate\frontend`: fronteira HTTP única, envelope, feedback global, permissões por capacidade, `nav-config` como fonte única, listagem paginada no servidor. A lib de UI foi trocada (Mantine → Tailwind v4 + shadcn/ui); `api/`, `auth/` e `i18n/` vieram iguais.

As **decisões por área** vivem em `.claude/rules/`, com front matter `paths:`, e carregam sozinhas quando você toca no código. **Ao planejar antes de abrir arquivo, leia a relevante de propósito.**

| Arquivo | Cobre |
|---|---|
| `.claude/rules/design-system.md` | tokens (único lugar de cor), tipografia Oswald + Inter, escuro por padrão, referências do Mobbin, contrato dos componentes `App*`, mobile primeiro |
| `.claude/rules/shell-e-navegacao.md` | `nav-config` como fonte única, sidebar/topbar/rail/bottom-nav, público × autenticado, admin no mesmo shell |
| `.claude/rules/auth.md` | telas de auth, gates do `RequireAuth`, a exceção do `EMAIL_NOT_CONFIRMED` inline, validação espelhando o Identity |
| `.claude/rules/login-social.md` | entrar com Google e Facebook: SDKs carregados só onde há botão, Facebook primeiro e o navegador do app do Facebook, `/entrar/apelido` e `/entrar/vincular` com o ticket no `state`, Formas de entrar e Criar senha em Minha conta, páginas `/privacidade` e `/apagar-dados` |
| `.claude/rules/bandas.md` | página da banda, criar e editar, integrantes (promover, remover, sair), convite pela busca de pessoas, convites no painel do sino, artista solo em `/artistas` (`bandPagePath`, textos `_solo`, "Novo projeto") e o "Costuma tocar com" |
| `.claude/rules/eventos.md` | agenda com abas e "Vou", detalhe com cancelamento, formulário com banner local e line-up misto, card de evento no feed, Shows da banda |
| `.claude/rules/feed.md` | feed único, composer com fotos e vídeo (upload multipart direto ao S3), publicar como banda, post em processamento com polling, editar e apagar, comentários, listas no perfil e na banda |
| `.claude/rules/mural.md` | linha do tempo interativa, envio da comunidade, meus envios, fila e painel da curadoria (`Archive.Review`), grade de registros, detalhe com legendas e "Eu estava lá", formulário com data aproximada e gerenciador de mídias de arquivo, permissões `Archive.*` |
| `.claude/rules/marcacoes.md` | @menção com autocomplete (`AppMentionTextarea`) e texto com links (`AppRichText`), marcação de pessoas nas fotos no `AppMediaViewer`, notificações de menção e marcação |
| `.claude/rules/ensaio.md` | Ensaio (músicos e bandas) no subdomínio `ensaio.`: moldura escolhida pelo endereço no mesmo build (`src/ensaio/frame.ts`, `/ensaio` em dev), visual "flyer limpo" com tokens só dentro de `.ensaio`, vitrine, ficha, vagas, interesse e convite, login com `voltar`, selo no perfil, vagas na banda e o card no feed |
| `.claude/rules/noticias.md` | página pública de notícias do Whiplash.Net: carrossel "Hoje" com scroll-snap, lista por dia com rolagem infinita, card que abre a matéria no site deles, foto pela URL deles, atribuição fixa, Notícias na bottom-nav do visitante |
| `.claude/rules/jobs-admin.md` | área de dev do admin em `/admin/jobs`: jobs agendados com a última execução, o resultado, o erro e o stack, "Rodar agora" com polling até terminar, aba de falhas do Hangfire |
| `.claude/rules/pwa.md` | instalável no celular: manifesto e ícones, service worker gerado no build (só o esqueleto, nunca a API), página sem conexão, aviso de versão nova, "Instalar o app" no menu do usuário e o passo a passo do iPhone |
| `.claude/rules/visitante.md` | o visitante sem conta: rotas `public` no `nav-config`, `RequireAuth allowVisitor`, shell sem sino nem busca, o que cada página aberta mostra, `AppSignInPrompt`, título da aba |
| `.claude/rules/compartilhamento.md` | `AppShareButton` (menu do sistema no celular, copiar link no computador), onde aparece, o link `/s/...` e o proxy do Vite |
| `.claude/rules/notificacoes.md` | SignalR só avisando (`api/notifications-hub.ts` + `layout/notifications-provider.tsx`), toast do push, balão do sino, frase pelo `kind` com plural de agrupamento, convites no topo, preferência de e-mail |
| `.claude/rules/suporte.md` | formulário com tipos, páginas e anexos, página de origem, "Meus relatos", conversa, caixa de entrada com triagem e contexto, permissões `Support.*` |
| `.claude/rules/mocks.md` | histórico: a forma do mock por página e o que morreu ao ligar cada módulo (não há mais mock) |
| `.claude/rules/perfil.md` | perfil público, edição em `/meu-perfil` separada da `/minha-conta` (o sensível), avatar, editor de capa, busca de pessoas, fixtures compartilhadas dos mocks |

**Rule nova entra nesta tabela no mesmo commit.**

## Stack

Vite 8 · React 19 · TypeScript 6 · Tailwind CSS v4 · shadcn/ui (base radix, preset nova) · react-hook-form + zod · sonner · lucide-react · axios · react-router-dom 7 · i18next.

| Lib | Uso |
|---|---|
| `tailwindcss` + `@tailwindcss/vite` | utilitários; tokens em `src/theme/tokens.css` via `@theme inline` |
| `shadcn` (CLI) + `radix-ui` | primitivos em `src/components/ui/*`, gerados pelo CLI e consumidos só pelos `App*` |
| `react-hook-form` + `zod` + `@hookform/resolvers` | formulários (`AppForm` + `AppField`); mensagens de erro são chaves do `common.json` |
| `sonner` | toasts (feedback global, via `toast-feedback.ts`) |
| `lucide-react` | ícones |
| `axios` | cliente HTTP (só em `src/api/`) |
| `react-router-dom` | rotas e guardas |
| `i18next` + `react-i18next` | tradução (só pt-BR por enquanto) |
| `@fontsource-variable/oswald` + `@fontsource-variable/inter` | fontes auto-hospedadas — **sem CDN** |

Comandos: `npm run dev` (porta 5173), `npm run build`, `npm run lint` (oxlint), `npm run typecheck`, `npm run preview`. Adicionar primitivo: `npx shadcn@latest add <nome>` (vai para `components/ui/`, ignorado pelo lint).

`VITE_API_BASE_URL` vem do `.env` (copie o `.env.example`). **Nunca hardcodar a URL da API.** `VITE_ENSAIO_URL` e `VITE_MAIN_URL` (vazios em dev) são os endereços do subdomínio do Ensaio e do site principal em produção (`ensaio.md`). `VITE_GOOGLE_CLIENT_ID` e `VITE_FACEBOOK_APP_ID` ligam os botões de login social; vazios, os botões somem (`login-social.md`). Sem a variável, o `vite.config.ts` derruba o build e o `client.ts` lança no boot.

## Estrutura de pastas

```
src/
  main.tsx                 # global.css, i18n, Toaster, TooltipProvider, Router, Auth, AppConfirm
  app.tsx                  # rotas: publicRoutes (PublicLayout) + definir-senha + bem-vindo + shell (RequireAuth > AppShell) + 404
  toast-feedback.ts        # liga o FeedbackSink da API ao sonner
  pwa/                     # registro do service worker (aviso de versão nova) e o "Instalar o app" (pwa.md)
  ensaio/                  # frame.ts (moldura pelo endereço, ensaioPath/ensaioUrl/mainUrl, voltar do login), boot.ts, EnsaioLink
  api/                     # fronteira HTTP — nada fora daqui usa axios (copiado do boilerplate)
    client.ts · auth-interceptor.ts · base-service.ts · api-codes.ts · feedback-sink.ts · token-storage.ts · types.ts · jwt.ts
    multipart-upload.ts    # PUT das partes de vídeo direto no S3 (axios próprio, sem auth e sem timeout)
    notifications-hub.ts   # conexão SignalR do sino (carregada sob demanda); só avisa a contagem
  auth/
    auth-context.ts · auth-provider.tsx · auth-service.ts · use-auth.ts
    require-auth.tsx       # unknown → skeleton; sem sessão → /entrar; senha pendente; boas-vindas (localStorage); permissão
    welcome-storage.ts     # app-welcome-seen:{userId} — quem já viu /bem-vindo
    redirect-if-authenticated.tsx
    permissions.ts · can.tsx · use-can.ts · roles.ts
    routes.ts              # caminhos das telas fora do shell + homePath/accountPath (ninguém escreve '/entrar' em página)
    validators.ts          # schemas zod reutilizados (senha, nickname, nome, e-mail) espelhando o Identity
  i18n/index.ts · pt-BR/*.json      # um namespace por área + common + api + layout
  hooks/                   # use-polling, use-media-query (useIsMobile), use-debounced-value
  lib/utils.ts             # cn() do shadcn
  theme/
    tokens.css             # ÚNICO lugar de cor; :root (claro), .dark (padrão), @theme inline
    global.css             # imports, base, utilitários app-h1/h2/h3, app-meta, app-page-enter
    color-scheme.ts        # read/apply/save + useColorScheme()
  components/
    ui/*                   # primitivos do shadcn (não importar de pages/ nem layout/)
    app-*/                 # globais com prefixo App (ver design-system.md)
  layout/
    nav-config.tsx         # FONTE ÚNICA: publicRoutes, navSections, hiddenRoutes, welcomeRoute
    nav-active.ts          # resolveActivePath
    app-shell.tsx · nav-sidebar.tsx · top-bar.tsx · bottom-nav.tsx · user-menu.tsx
    shell-rail.tsx · shell-rail-context.ts · use-shell-rail.ts   # rail direito registrado pela página
    notifications-provider.tsx · notifications-context.ts · use-notifications.ts   # contador do sino, push e toast
    last-location.ts       # última rota do shell antes do /reportar-problema (a "página de onde veio" do relato)
    public-layout.tsx      # PublicFrame (card centralizado) + PublicLayout (Outlet)
    wordmark.tsx
    ensaio/                # shell, navegação e rotas do Ensaio (outra moldura, fora do nav-config)
  pages/
    <secao>/               # uma pasta por tela; cada tela um arquivo
      index.tsx | <tela>.tsx
      components/ hooks/ services/   # locais da tela; services/mocks.ts enquanto o módulo não existe
      <x>-status.ts        # rótulo/tom derivados de enum
```

Alias de import: `@/` aponta para `src/` (desvio do "imports relativos" do boilerplate: o CLI do shadcn gera com `@/` e misturar dois estilos é pior). Dentro de `auth/` e `layout/`, que vieram do boilerplate, os imports relativos entre vizinhos continuam.

### Onde cada coisa mora

- **Página** é `pages/<secao>/index.tsx` ou `pages/<secao>/<tela>.tsx`, com `components/`, `hooks/` e `services/` **locais** ao lado.
- **Service nunca é promovido.** Um service vive na página dele e fica lá mesmo quando outra página passa a usá-lo — a segunda importa do caminho da primeira. **Não existe `src/services/`.** A exceção é `auth/auth-service.ts`: sessão não pertence a uma página.
- **Componente global** mora em `src/components/app-*/` e leva o prefixo `App`. **Sem prefixo, é local da página.** `components/ui/*` é primitivo e só os `App*` o importam.
- **Rótulo, tom e formatação derivados de enum** moram em `pages/<secao>/<x>-status.ts` com `Record<Enum, …>` exaustivo — o build quebra quando o backend ganha estado novo.
- `layout/` é a moldura, não componente reutilizável — por isso fica fora de `components/` e sem prefixo.

## Camada de API

Regra central: **toda comunicação HTTP fica em `src/api/`**. Página nenhuma importa axios — sempre através de um service que usa o `base-service`.

- **Envelope**: toda resposta é `ApiEnvelope<T>` = `{ isSuccess, code, message, data }`.
- **`base-service.ts`** expõe `apiGet/apiPost/apiPut/apiPatch/apiDelete/apiPostForm<T>`: desembrulha o envelope e, em falha, lança `ApiError(code)`. Quem consome nunca vê o envelope.
- **Feedback é global e sai do `base-service`**: **erro toasta sozinho, sucesso é opt-in** (`toastSuccess: true`). Página nenhuma toasta erro de API. A única exceção registrada está em `auth.md`.
- **Sessão por cookie** (`auth.md`): o refresh token vive num cookie `HttpOnly` que o JavaScript não lê, e o access token de **15 minutos** fica **só em memória** (`token-storage.ts`). O `client.ts` manda `withCredentials: true`; no boot, `restoreSessionToken()` troca o cookie por um token. Renovações de abas diferentes passam em fila pelo Web Locks (`nv-refresh`); o `auth-interceptor` renova em 401 e repete a request; 401 concorrentes compartilham **uma** renovação; se o refresh falhar, limpa e dispara `onSessionExpired`.
- `validateStatus: status < 500`: 4xx chega como resposta normal; 5xx e rede viram `NETWORK_ERROR`/`UNKNOWN_ERROR`.

## Navegação — `nav-config.tsx` é a fonte única

Rota e item de menu **nascem do mesmo lugar**. `publicRoutes` (com `guestOnly` para redirecionar quem já tem sessão), `navSections` (sidebar e bottom-nav; `mobile: true` marca os 4 fixos da bottom-nav; `mobilePath` resolve rota dinâmica como `/perfil/:nickname`), `hiddenRoutes` (telas sem item) e `welcomeRoute`. `app.tsx` só mapeia. Detalhes em `shell-e-navegacao.md`.

**Nunca adicione uma rota direto no `app.tsx` nem um item direto na sidebar ou bottom-nav.**

## Listagem: paginação e filtros

**Toda listagem nasce paginada** no servidor (ou no mock, pelo `mockPage`). `PagedResult<T>` em `api/types.ts`; `emptyPage<T>()` para o estado inicial. `AppPagination` é o único paginador (`mode: 'pages' | 'loadMore' | 'infinite'`); `defaultPageSize = 10`, teto 100. O estado de página/filtros mora num **hook local** (`hooks/use-<x>-page.ts`) que devolve `{ items, page, …, loading, change<Filtro>, setPage, refresh, reload }`. **Toda mudança de filtro reseta a página para 1** (por isso `changeX`, não `setX`). Busca textual passa por `useDebouncedValue` (350 ms). Ação que muda a lista chama o service e o hook **refetcha** — nunca edita o array local.

## Perfis e acesso

`POST /api/auth/login` é o mesmo para qualquer usuário; **qualquer `User` entra** na comunidade. `Admin.Access` só abre as telas de administração; `fetchMyAccess` tolera falha (`access` vazio).

🔴 **O gate do front é UX, não segurança.** Quem protege dado são as policies de cada endpoint. Telas e rotas checam **capacidade** (`auth/permissions.ts`, `useCan()`, `<Can>`, `permission` no `nav-config`), nunca nome de papel.

## i18n

Só **pt-BR** por enquanto, mas nada de string solta: **nenhuma string de UI é hardcoded.** `grep -rnP "[áéíóúâêôãõç]" src --include=*.ts --include=*.tsx | grep -v src/i18n/ | grep -v api/client.ts | grep -v services/mocks.ts` deve voltar vazio (as fixtures dos mocks são dado de exemplo e morrem com o módulo). Um namespace por área (`auth`, `conta`, `feed`, …) mais `common`, `layout` e `api`. **O `code` do envelope é a chave de tradução** (`api-codes.ts` + `api.json`); mensagem de validação do zod é chave de `common.json` (`validation.*`) traduzida pelo `AppField`.

## UI e tema

🔴 **Nenhuma cor fora de `theme/tokens.css`**; 🔴 **botão é sempre `AppButton`**; campo é `AppField`/`AppPasswordField`/`AppTextareaField` dentro de `AppForm`; confirmação destrutiva é `useAppConfirm()` (recebe `onConfirm`, não devolve booleano); avatar é `AppAvatar`; badge é `AppBadge`; vazio é `AppEmptyState`; carregando é `AppSkeleton`; cabeçalho é `AppPageHeader`; paginação é `AppPagination`; modal é `AppDialog`. Tema escuro por padrão, trocado só no `user-menu`; o script no `<head>` do `index.html` aplica antes do paint. Tudo em `design-system.md`.

## Fluxo canônico de feature

```
página → hook (estado local) → service da página (real ou mocks.ts) → base-service → axios
→ ApiEnvelope → DTO desembrulhado | ApiError + toast global
```

A página **não trata o erro**: o toast já saiu; o hook só devolve `false` ou estado vazio. Estado que atravessa páginas (sessão) vive em Context; estado de uma tela fica no hook dela. Não há biblioteca de cache de dados — cada hook busca o que precisa, com guarda `cancelled` no `useEffect`.

### Checklist: nova página

1. Pasta `src/pages/<secao>/` com `index.tsx` (ou `<tela>.tsx`).
2. `services/<secao>-service.ts` com DTOs tipados; se o módulo não existe no backend, `services/mocks.ts` com a mesma assinatura (`mocks.md`).
3. `hooks/use-<algo>-page.ts` com o estado local.
4. `components/` para o que for só daquela tela; global → `src/components/app-*/`.
5. Namespace `src/i18n/pt-BR/<secao>.json` registrado em `i18n/index.ts`; codes novos em `api-codes.ts` + `api.json`.
6. Capacidade em `auth/permissions.ts` espelhando o backend, se for tela de admin.
7. Rota **e** item no `layout/nav-config.tsx`; detalhe/formulário em `hiddenRoutes` + `subRoutes`.
8. Conferir a 390px e a 1440px.

### O que atualizar depois de codar

- Decisão de tela não óbvia → rule da área; rule nova → tabela no topo.
- Code novo → `api-codes.ts` + `api.json`.
- Mock novo → tabela em `mocks.md`.
- Armadilha resolvida fica ~~tachada~~; divergência doc↔código se registra, não se apaga.

## Convenções gerais

- Arquivos e pastas em `kebab-case`; componentes em `PascalCase`; hooks `useX`; DTOs `PascalCase` com sufixo `Dto`.
- **Não escrever comentários no código**, nem JSDoc. O que precisa de explicação vem para este documento ou para a rule.
- Código em inglês; a URL que a pessoa vê fica em português (`/criar-conta`, `/eventos`).
- Enum do backend chega como número e vira `const` + `Record` exaustivo na página dona.
- `.env` não vai para o Git (só `.env.example`).
- Mock é andaime descartável: ao ligar o endpoint real, o `mocks.ts` morre no mesmo commit.

## Como o agente trabalha aqui

### Antes de escrever código

1. Ler "Sobre este projeto", este arquivo e a rule da área. Tela ligada a backend: ler também a rule irmã em `../api/.claude/rules/` — a regra é do backend, a tela só reflete.
2. Confirmar se o endpoint existe. Se não existe, a tela nasce com `mocks.ts` e o DTO do mock é a proposta de contrato; se existe, o DTO é o do backend.
3. Escolher a referência: formulário de auth → `pages/criar-conta`; formulário de criação ou edição → `AppFormLayout` com prévia ao vivo (`pages/eventos/form.tsx`, `pages/ensaio/minha-ficha.tsx`); estados de sucesso → `pages/confirmar-email`; conta → `pages/minha-conta`.

### Durante

- Copiar a estrutura da referência. Nada de arquitetura paralela (segundo cliente HTTP, segundo paginador, segundo toast, segunda paleta).
- Cada string no i18n; cada cor no tema; cada botão `AppButton`; cada filtro resetando a página.
- Não editar array local para simular resultado — refetch (o mock muta a fixture).

### Depois

- `npm run lint`, `npm run typecheck`, `npm run build`; greps de cor, de acentuação e de `ui/button`.
- Verificação no browser a 390px e 1440px, com a API e o Mailpit no ar para as telas de auth.
- Atualizar doc no mesmo commit.

### Hierarquia de regras

1. Rule da área (`.claude/rules/*.md`).
2. Este `CLAUDE.md`.
3. `CLAUDE.md` da API (contrato: codes, permissões, paginação).
4. Convenção da lib.

## Verificação

Sem testes automatizados nesta entrega: `lint` + `typecheck` + `build` + greps + roteiro das rules no browser. O roteiro ponta a ponta de auth está em `auth.md`; um script Playwright que o reproduz vive fora do repositório enquanto não houver decisão de testar no CI.

## Armadilhas conhecidas

- **O gate de papel não é controle de acesso** — é enfeite se o endpoint não estiver protegido.
- **Access token de 15 minutos.** Qualquer coisa que contorne o `auth-interceptor` derruba o usuário no meio do trabalho.
- `AppButton asChild` (para `<Link>`) não renderiza o spinner de `loading`: o `Slot` do radix exige um único filho.
- `components/ui/*` exporta variantes (`buttonVariants`) além de componentes; por isso a pasta está em `ignorePatterns` do oxlint.
- O `Toaster` usa `useColorScheme()` em vez de `next-themes`; trocar o tema sem passar por `saveColorScheme` deixa o toast na cor errada.
- `auth-provider.tsx` faz `setState` dentro do `useEffect` de boot (aviso `set-state-in-effect` do oxlint): herdado do boilerplate, deliberado — o boot é a troca do cookie por um token na API.
- O CORS da API vem de `Cors:AllowedOrigins` (dev libera só `http://localhost:5173`). Se o Vite subir em outra porta, toda request falha por CORS.
- O cookie da sessão é `SameSite=Strict`: o front e a API precisam ser o mesmo site. Em dev, os dois em `localhost` (porta não conta); abrir o front em `127.0.0.1` com a API em `localhost`, ou o contrário, faz o cookie não ir e todo recarregar cai em `/entrar`.
- O `vite.config.ts` repassa `^/s/` para a API (prévia de link, `compartilhamento.md`). Rota nova do front que comece com `/s/` vai parar na API.

## Como este arquivo evolui

Convenção que vale para qualquer frontend volta para o boilerplate; regra da tela deste produto fica na rule. Se apagar este projeto e a frase continuar verdadeira, é convenção.
