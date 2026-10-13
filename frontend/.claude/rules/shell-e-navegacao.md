---
paths:
  - "src/layout/**"
  - "src/app.tsx"
  - "src/main.tsx"
---

# Decisões — Shell e navegação

## `nav-config.tsx` é a fonte única de rota, menu e permissão

`layout/nav-config.tsx` exporta três listas: `publicRoutes` (fora do shell: auth, confirmação, redefinição; a `/bem-vindo` também fica fora do shell, em `welcomeRoute`), `navSections` (o que aparece na sidebar e na bottom-nav, com `path`, `labelKey`, `icon`, `element`, `permission?`, `subRoutes?`, `mobile?`) e `hiddenRoutes` (telas sem item de menu: detalhe, formulário, perfil de terceiro, conta). `app.tsx` mapeia as três; `nav-sidebar.tsx` e `bottom-nav.tsx` leem `navSections`.

🔴 **Nunca adicione uma rota direto no `app.tsx` nem um item direto na sidebar ou na bottom-nav.** Item de menu só existe quando a tela existe (mesmo que mockada). Criação é rota própria (`/eventos/novo`), não modal; detalhe e edição vão em `hiddenRoutes` e o item pai declara `subRoutes` para continuar marcado.

Item ativo: `resolveActivePath` (copiado do boilerplate) resolve **uma vez** para o menu inteiro, primeiro por `path` exato, depois por `subRoutes`. `pathname.startsWith` marcaria `/eventos` e `/eventos/novo` ao mesmo tempo.

## Três regiões em desktop, duas em mobile

- **Sidebar** (≥ `md`, 240px): wordmark, `navSections`, e no rodapé o `user-menu` (avatar + nome + `@nickname`; menu com Minha conta, Tema, Sair). Sem modo colapsado nesta entrega: o site não é ferramenta de trabalho em que a pessoa passa o dia — menos estado, menos bug.
- **Topbar**: busca (mock, somente leitura com tooltip), sino de notificações (`notifications-bell.tsx`: contador mock, refetch a cada navegação e `usePolling` de 30 s), e em mobile o wordmark à esquerda e o avatar à direita.
- **`<main>`**: coluna de até 680px centrada em páginas de leitura (feed, detalhe); largura total em galerias e listas. A página escolhe com `AppPageHeader`/`contentWidth`.
- **Rail direito** (≥ `xl`, 320px): a página registra o conteúdo via `useShellRail(node)` (`shell-rail.tsx`); vazio = não renderiza. Feed registra "Próximos eventos" e "Bandas sugeridas".
- **Bottom-nav** (< `md`): os 4 itens com `mobile: true` em `navSections` (Feed, Vídeos, Eventos, Perfil) mais "Mais", que abre um `Sheet` com todas as outras folhas agrupadas por seção. O item "Mais" acende quando a rota ativa está dentro dele.

🔴 **`user-menu.tsx` é o único dono de tema e "Sair".** Não recriar em outro lugar.

## Público × autenticado

Rotas públicas usam `PublicLayout` (fundo escuro, card centralizado, wordmark). Rotas do shell passam por `RequireAuth` (ver `auth.md`). Rota pública com sessão ativa redireciona para `/feed` (`redirect-if-authenticated.tsx`), exceto `/confirmar-email` e `/redefinir-senha`, que precisam funcionar com link aberto em sessão de outra conta.

## Admin dentro do mesmo shell

`/admin/moderacao` é um item de `navSections` com `permission: Admin.Access`; a sidebar o esconde para quem não tem. O gate do front é UX; a proteção é a policy do endpoint. Sem permissão, a rota redireciona para `/feed`.

## Roteiro de verificação

1. A 1440px: sidebar + topbar + coluna + rail no `/feed`; rail some a 1279px; sidebar vira bottom-nav a 767px.
2. Navegar `/eventos` → `/eventos/123`: o item "Eventos" continua ativo; `/eventos/novo` idem.
3. Sem sessão, qualquer rota do shell cai em `/entrar` e, depois do login, volta para a rota pedida.
4. Com sessão, `/entrar` redireciona para `/feed`; `/redefinir-senha?token=x` abre normalmente.
5. Usuário comum não vê "Moderação"; admin vê. Acessar `/admin/moderacao` sem permissão → `/feed`.
