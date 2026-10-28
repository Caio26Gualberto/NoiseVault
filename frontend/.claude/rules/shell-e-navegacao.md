---
paths:
  - "src/layout/**"
  - "src/app.tsx"
  - "src/main.tsx"
---

# Decisões — Shell e navegação

## `nav-config.tsx` é a fonte única de rota, menu e permissão

`layout/nav-config.tsx` exporta três listas: `publicRoutes` (fora do shell: auth, confirmação, redefinição; a `/bem-vindo` e a `/regras` também ficam fora do shell, em `welcomeRoute` e `rulesRoute`), `navSections` (o que aparece na sidebar e na bottom-nav, com `path`, `labelKey`, `icon`, `element`, `permission?`, `subRoutes?`, `mobile?`) e `hiddenRoutes` (telas sem item de menu: detalhe, formulário, perfil de terceiro, conta). `app.tsx` mapeia as três; `nav-sidebar.tsx` e `bottom-nav.tsx` leem `navSections`.

🔴 **Nunca adicione uma rota direto no `app.tsx` nem um item direto na sidebar ou na bottom-nav.** Item de menu só existe quando a tela existe. Criação é rota própria (`/eventos/novo`), não modal; detalhe e edição vão em `hiddenRoutes` e o item pai declara `subRoutes` para continuar marcado.

Item ativo: `resolveActivePath` (copiado do boilerplate) resolve **uma vez** para o menu inteiro, primeiro por `path` exato, depois por `subRoutes`. `pathname.startsWith` marcaria `/eventos` e `/eventos/novo` ao mesmo tempo.

## Três regiões em desktop, duas em mobile

- **Sidebar** (≥ `md`, 240px): wordmark, `navSections`, e no rodapé o `user-menu` (avatar + nome + `@nickname`; menu com Meu perfil, Editar perfil, Minha conta, Ensaio, "Sobre o Noise Vault", "Regras da comunidade", Tema, Sair). A página Sobre saiu da seção Institucional da sidebar para o menu da conta (pedido do usuário em 15/10/2026): é sobre quem fez o site, não uma área do produto; a rota `/sobre` continua em `hiddenRoutes`. Sem modo colapsado nesta entrega: o site não é ferramenta de trabalho em que a pessoa passa o dia — menos estado, menos bug.
- **Topbar**: busca de pessoas real (`people-search.tsx`, só a partir de `md`; ver `perfil.md`), sino de notificações, que **abre um balão** (`Popover`) com a lista (desde 14/10/2026 não há página `/notificacoes` nem item na sidebar ou no "Mais"; `notificacoes.md`) (`notifications-bell.tsx`, que lê o `NotificationsProvider` do `AppShell`: contador real empurrado por SignalR, com polling de 60 s só enquanto a conexão está caída — `notificacoes.md`), e em mobile o wordmark à esquerda e o avatar à direita.
- **`<main>`**: coluna de até 680px centrada em páginas de leitura (feed, detalhe); largura total em galerias e listas. A página escolhe com `AppPageHeader`/`contentWidth`.
- **Rail direito** (≥ `xl`, 320px): a página registra o conteúdo via `useShellRail(node)` (`shell-rail.tsx`); vazio = não renderiza. Feed registra "Próximos eventos" e "Bandas sugeridas".
- **Bottom-nav** (< `md`): os 4 primeiros itens com `mobile: true` em `navSections` (Feed, Mural, Eventos, Perfil; Notícias também é `mobile`, mas vem depois, então só entra na barra do visitante, que não tem Perfil — `noticias.md`) mais "Mais", que abre um `Sheet` com todas as outras folhas agrupadas por seção. O item "Mais" acende quando a rota ativa está dentro dele.
  - **Só ícones** (pedido do usuário em 21/10/2026): o nome da página saiu da barra, e os ícones passaram de 20 para 24 px. O nome continua num `sr-only`, que é o nome acessível do link para o leitor de tela. O "Mais" e o "Entrar" do visitante seguem a mesma regra. Dentro do `Sheet` do "Mais", os itens continuam com ícone e nome.

🔴 **`user-menu.tsx` é o único dono de tema e "Sair".** Não recriar em outro lugar. A exceção é o cabeçalho do Ensaio, que tem o próprio menu da conta porque os links do `user-menu` não funcionam no subdomínio (`ensaio.md`). O `user-menu` também tem o "Instalar o app", que só aparece quando dá para instalar (`pwa.md`).

## Público × autenticado

Rotas públicas usam `PublicLayout` (fundo escuro, card centralizado, wordmark). Rotas do shell passam por `RequireAuth` (ver `auth.md`). **Desde 20/10/2026, as rotas do shell marcadas com `public: true` (agenda, evento, mural, registro, banda) abrem sem sessão**, no mesmo shell em modo visitante (`visitante.md`). Rota pública com sessão ativa redireciona para `/feed` (`redirect-if-authenticated.tsx`), exceto `/confirmar-email` e `/redefinir-senha`, que precisam funcionar com link aberto em sessão de outra conta.

## O Ensaio é outra moldura

`/ensaio/*` (e a raiz de `ensaio.noisevault.net`) não passa pelo `AppShell` nem pelo `nav-config`: tem shell, navegação (`layout/ensaio/ensaio-nav.ts`) e rotas próprias (`layout/ensaio/ensaio-routes.tsx`). O `app.tsx` monta a moldura do Ensaio na frente das outras rotas. Tudo em `ensaio.md`.

## Admin dentro do mesmo shell

A seção "Administração" saiu com a moderação (módulo 5 redefinido) e **voltou com o suporte** (14/10/2026): `navSections` tem `admin` com os itens "Usuários" (`/admin/usuarios`, `Authorization.Users.View`, `usuarios-admin.md`, desde 27/10/2026), "Jobs" (`/admin/jobs`, `System.Jobs.View`, `jobs-admin.md`, desde 21/10/2026), "Verificação de bandas" e estes dois: "Envios do mural" (`/admin/mural`, `Archive.Review`) e "Suporte" (`/admin/suporte`, sub-rota `/admin/suporte/:id`, `Support.View`).
- **A seção não tem permissão própria**: cada item leva a sua, e a sidebar e o "Mais" escondem a seção quando não sobra nenhum item. Assim um curador do mural vê a Administração sem precisar de `Support.View`.
- Seção nova de administração entra nesse mesmo bloco, cada item com a sua permissão.

Rotas com `permission` em `hiddenRoutes` (`/mural/novo` com `Archive.Create`, `/admin/suporte/:id` com `Support.View`) redirecionam para `/feed` sem a capacidade. O gate do front é UX; a proteção é a policy do endpoint.

## Roteiro de verificação

1. A 1440px: sidebar + topbar + coluna + rail no `/feed`; rail some a 1279px; sidebar vira bottom-nav a 767px.
2. Navegar `/eventos` → `/eventos/123`: o item "Eventos" continua ativo; `/eventos/novo` idem.
3. Sem sessão, qualquer rota do shell que não seja `public` cai em `/entrar` e, depois do login, volta para a rota pedida; as `public` abrem em modo visitante (`visitante.md`).
4. Com sessão, `/entrar` redireciona para `/feed`; `/redefinir-senha?token=x` abre normalmente.
5. Usuário comum não vê "Novo registro" no mural; admin vê. Acessar `/mural/novo` sem permissão → `/feed`.
