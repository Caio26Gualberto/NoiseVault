# CLAUDE.md — Frontend (web)

## Sobre este projeto

**Noise Vault** — site público da cena do ABC Paulista (metal, punk, hardcore e afins): feed, vídeos, fotos, eventos, perfis de pessoas e de bandas, notificações. Quem usa é o fã e o músico da região; a administração (moderação, equipe) é a mesma aplicação com permissão. Reescrita do zero do legado "NewLevel" (`C:\Projetos\Pessoal\Alvo\newlevel`), que não serve de referência de código nem de tela.

**Estado da entrega atual:** auth é real (`/entrar`, `/criar-conta`, `/confirmar-email`, `/esqueci-senha`, `/redefinir-senha`, `/definir-senha`, `/bem-vindo`); perfil e mídia também (`/minha-conta` inteira com avatar e capa, `/perfil/:nickname`, busca de pessoas na topbar — `.claude/rules/perfil.md`). Feed, vídeos, fotos, eventos, bandas, notificações, moderação, reportar problema e institucionais são **mock por página** com a assinatura que o service real vai ter (`.claude/rules/mocks.md`). A ordem em que os mocks viram backend está em `../api/.claude/rules/dominio.md`.

## Como trabalhar neste repositório

Arquitetura e convenções do frontend. **As telas de referência são `pages/criar-conta`** (formulário com zod, estados de sucesso, service real) **e `pages/eventos`** (listagem paginada com filtros em `hooks/use-eventos-page.ts`, detalhe, formulário em rota própria, tudo sobre `mocks.ts`). Quando tiver dúvida, copie a estrutura delas.

Este projeto consome a API (`../api` — ver o `CLAUDE.md` de lá). **A interface muda a cada projeto; a arquitetura não.** O esqueleto veio do boilerplate em `C:\Users\Administrador\Desktop\Boilerplate\frontend`: fronteira HTTP única, envelope, feedback global, permissões por capacidade, `nav-config` como fonte única, listagem paginada no servidor. A lib de UI foi trocada (Mantine → Tailwind v4 + shadcn/ui); `api/`, `auth/` e `i18n/` vieram iguais.

As **decisões por área** vivem em `.claude/rules/`, com front matter `paths:`, e carregam sozinhas quando você toca no código. **Ao planejar antes de abrir arquivo, leia a relevante de propósito.**

| Arquivo | Cobre |
|---|---|
| `.claude/rules/design-system.md` | tokens (único lugar de cor), tipografia Oswald + Inter, escuro por padrão, referências do Mobbin, contrato dos componentes `App*`, mobile primeiro |
| `.claude/rules/shell-e-navegacao.md` | `nav-config` como fonte única, sidebar/topbar/rail/bottom-nav, público × autenticado, admin no mesmo shell |
| `.claude/rules/auth.md` | telas de auth, gates do `RequireAuth`, a exceção do `EMAIL_NOT_CONFIRMED` inline, validação espelhando o Identity |
| `.claude/rules/mocks.md` | a forma do mock por página, o que apagar ao ligar cada módulo |
| `.claude/rules/perfil.md` | perfil público, edição em `/minha-conta`, avatar, editor de capa, busca de pessoas, fixtures compartilhadas dos mocks |

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

`VITE_API_BASE_URL` vem do `.env` (copie o `.env.example`). **Nunca hardcodar a URL da API.** Sem a variável, o `vite.config.ts` derruba o build e o `client.ts` lança no boot.

## Estrutura de pastas

```
src/
  main.tsx                 # global.css, i18n, Toaster, TooltipProvider, Router, Auth, AppConfirm
  app.tsx                  # rotas: publicRoutes (PublicLayout) + definir-senha + bem-vindo + shell (RequireAuth > AppShell) + 404
  toast-feedback.ts        # liga o FeedbackSink da API ao sonner
  api/                     # fronteira HTTP — nada fora daqui usa axios (copiado do boilerplate)
    client.ts · auth-interceptor.ts · base-service.ts · api-codes.ts · feedback-sink.ts · token-storage.ts · types.ts · jwt.ts
    mock-delay.ts          # andaime dos mocks: delay() + mockPage(); morre com o último mocks.ts
    mock-fixtures.ts       # pessoas e bandas de exemplo que os mocks compartilham; morre com o último mocks.ts
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
    public-layout.tsx      # PublicFrame (card centralizado) + PublicLayout (Outlet)
    wordmark.tsx
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
- **Tokens** no `localStorage` (`token-storage.ts`). Access token de **15 minutos**; o `auth-interceptor` renova em 401 e repete a request; 401 concorrentes compartilham **uma** renovação; se o refresh falhar, limpa e dispara `onSessionExpired`.
- `validateStatus: status < 500`: 4xx chega como resposta normal; 5xx e rede viram `NETWORK_ERROR`/`UNKNOWN_ERROR`.

## Navegação — `nav-config.tsx` é a fonte única

Rota e item de menu **nascem do mesmo lugar**. `publicRoutes` (com `guestOnly` para redirecionar quem já tem sessão), `navSections` (sidebar e bottom-nav; `mobile: true` marca os 4 fixos da bottom-nav; `mobilePath` resolve rota dinâmica como `/perfil/:nickname`), `hiddenRoutes` (telas sem item) e `welcomeRoute`. `app.tsx` só mapeia. Detalhes em `shell-e-navegacao.md`.

**Nunca adicione uma rota direto no `app.tsx` nem um item direto na sidebar ou bottom-nav.**

## Listagem: paginação e filtros

**Toda listagem nasce paginada** no servidor (ou no mock, pelo `mockPage`). `PagedResult<T>` em `api/types.ts`; `emptyPage<T>()` para o estado inicial. `AppPagination` é o único paginador (`mode: 'pages' | 'loadMore'`); `defaultPageSize = 10`, teto 100. O estado de página/filtros mora num **hook local** (`hooks/use-<x>-page.ts`) que devolve `{ items, page, …, loading, change<Filtro>, setPage, refresh, reload }`. **Toda mudança de filtro reseta a página para 1** (por isso `changeX`, não `setX`). Busca textual passa por `useDebouncedValue` (350 ms). Ação que muda a lista chama o service e o hook **refetcha** — nunca edita o array local.

## Perfis e acesso

`POST /api/auth/login` é o mesmo para qualquer usuário; **qualquer `User` entra** na comunidade. `Admin.Access` só abre as telas de administração; `fetchMyAccess` tolera falha (`access` vazio).

🔴 **O gate do front é UX, não segurança.** Quem protege dado são as policies de cada endpoint. Telas e rotas checam **capacidade** (`auth/permissions.ts`, `useCan()`, `<Can>`, `permission` no `nav-config`), nunca nome de papel.

## i18n

Só **pt-BR** por enquanto, mas nada de string solta: **nenhuma string de UI é hardcoded.** `grep -rnP "[áéíóúâêôãõç]" src --include=*.ts --include=*.tsx | grep -v src/i18n/ | grep -v api/client.ts | grep -v services/mocks.ts | grep -v api/mock-fixtures.ts` deve voltar vazio (as fixtures dos mocks são dado de exemplo e morrem com o módulo). Um namespace por área (`auth`, `conta`, `feed`, …) mais `common`, `layout` e `api`. **O `code` do envelope é a chave de tradução** (`api-codes.ts` + `api.json`); mensagem de validação do zod é chave de `common.json` (`validation.*`) traduzida pelo `AppField`.

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
3. Escolher a referência: formulário → `pages/criar-conta`; estados de sucesso → `pages/confirmar-email`; conta → `pages/minha-conta`.

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
- `auth-provider.tsx` faz `setState` dentro do `useEffect` de boot (aviso `set-state-in-effect` do oxlint): herdado do boilerplate, deliberado — o boot é sincronização com `localStorage`.
- O CORS da API vem de `Cors:AllowedOrigins` (dev libera só `http://localhost:5173`). Se o Vite subir em outra porta, toda request falha por CORS.

## Como este arquivo evolui

Convenção que vale para qualquer frontend volta para o boilerplate; regra da tela deste produto fica na rule. Se apagar este projeto e a frase continuar verdadeira, é convenção.
