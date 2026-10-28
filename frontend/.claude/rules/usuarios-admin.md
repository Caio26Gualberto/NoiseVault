---
paths:
  - "src/pages/admin/usuarios/**"
  - "src/i18n/pt-BR/usuarios.json"
---

# Decisões — Tela de usuários (`/admin/usuarios`)

Pedido do usuário em 27/10/2026, depois de anunciar o site: ver quantas pessoas estão se cadastrando. A regra e os endpoints (`GET /api/admin/users` e `/summary`) estão em `../api/.claude/rules/auth-e-contas.md` (seção "Lista de usuários do admin").

## O que a tela mostra

- **Item "Usuários"** na seção Administração (`UsersIcon`), com `permission: Permissions.authorizationUsersView`. Hoje só o Admin tem.
- **Quatro números no topo**: Hoje, Últimos 7 dias, Últimos 30 dias e Total, em grade de 2 colunas no celular e 4 a partir de `md`, em `tabular-nums`. Embaixo, "N contas ainda não confirmaram o e-mail" quando há alguma: depois de um anúncio, é ali que se vê quem parou no meio do cadastro. Enquanto o resumo carrega, os números aparecem como "–", e a linha de baixo guarda a altura para nada pular.
- **Busca por nome ou apelido** (o mesmo input da verificação de bandas, debounce de 350 ms, volta à página 1). Nunca pelo e-mail, que a API não procura.
- **A lista**, do cadastro mais novo para o mais antigo, paginada (`AppPagination` em `pages`). Cada linha mostra:
  - o avatar, o nome com link para o perfil, o `@apelido` e o e-mail;
  - "Entrou há 2 horas · Santo André", com a data completa no `title`;
  - os badges de como entra: "E-mail e senha" (neutro), Google ou Facebook (`info`, nomes de `providerNames`) e "E-mail não confirmado" (`warning`).
- **Só leitura.** Revogar acesso e trocar papel ficam para quando houver pedido. A tela não ganha botão antes da ação existir.
- **Campos tolerados ausentes** (`avatarUrl`, `cityName`, `emailConfirmed`, `hasPassword`, `providers`): o front sobe antes da API no deploy (`DEPLOY.md`), e por um minuto a lista vem sem eles. Sem os campos, a linha fica sem badges: o "não confirmado" só aparece com `emailConfirmed === false` explícito.

## Roteiro de verificação

Com a API no ar, logado como admin:

1. A sidebar mostra Administração → Usuários; uma conta comum não vê o item e, em `/admin/usuarios`, cai no `/feed`.
2. Os quatro números batem com `GET /api/admin/users/summary`. Criar uma conta em `/criar-conta` sem confirmar e recarregar: Hoje, Total e "não confirmaram" sobem 1, e a conta aparece no topo da lista com "E-mail e senha" e "E-mail não confirmado".
3. Buscar "noise" filtra pelas contas de seed; buscar algo que não existe mostra "Ninguém encontrado".
4. Uma conta que entrou pelo Google mostra o badge Google.
5. A 390px: os números em 2 colunas, o e-mail longo cortado com reticências, sem rolagem horizontal.
