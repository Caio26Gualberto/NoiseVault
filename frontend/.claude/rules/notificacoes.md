---
paths:
  - "src/pages/notificacoes/**"
  - "src/layout/notifications-*.tsx"
  - "src/layout/notifications-*.ts"
  - "src/layout/use-notifications.ts"
  - "src/api/notifications-hub.ts"
---

# Decisões — Notificações no frontend

Rule irmã: `../api/.claude/rules/notificacoes.md`. A regra é do backend; a tela só reflete. Codes em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`.

## Tempo real: SignalR avisa, o REST lista

- **`src/api/notifications-hub.ts`** (dentro da fronteira HTTP) abre a conexão em `/hubs/notifications`:
  - `@microsoft/signalr` carregado sob demanda (`import()`, fora do bundle inicial, como o hls.js);
  - só WebSocket (`skipNegotiation`) e `withAutomaticReconnect()`.
  - O token vem de `getFreshToken()` (`api/client.ts`): o access token do `localStorage`, renovado pelo mesmo `refreshOnce` do interceptor quando vence em menos de 60 s.
- **`layout/notifications-provider.tsx`**, montado no `AppShell` (só existe com sessão), guarda o `unreadCount` e uma `version`.
  - **Mensagem `unread` do hub**: troca o número. Se ele **subiu** e a pessoa não está em `/notificacoes`, busca a notificação não lida mais recente e mostra um toast com a frase e a ação "Ver", que marca como lida e navega.
  - **Contagem mudou**: a `version` sobe, e a lista e os convites refazem a busca.
  - **Conexão caída**: o provider faz polling da contagem a cada 60 s (`usePolling` com 0 quando conectado) e tenta reconectar a cada 30 s. Ao conectar, busca a contagem de novo.
- **Busca em segundo plano** (contagem e "a mais recente") usa `toastError: false`: a API fora do ar não pode encher a tela de toast a cada minuto.
- O toast do push é a exceção registrada à regra "página não chama `toast()`": não é erro de API, é aviso, e não tem dono melhor que o provider.
- O **sino** (`notifications-bell.tsx`) só lê `useNotifications()`.

## A frase vem do `kind`

- `notification-kind.ts` espelha o `ENotificationKind`.
- `notification-message.ts` tem o `Record<NotificationKind, { key, icon, link }>` exaustivo e monta a chave e os valores da frase:
  - `count` = `actorCount - 1`, para os plurais `_zero`/`_one`/`_other`: "**Fulano** curtiu", "**Fulano** e mais 1 pessoa curtiram", "**Fulano** e mais 3 pessoas curtiram";
  - o convite com instrumento usa `bandInviteInstrument`.
- As frases ficam em `notificacoes.json` (`kinds.*`), com `<b>` nos nomes. A lista renderiza com `<Trans>`; o toast usa `notificationPlainText`, que tira as tags.
- **Links**:
  - post → `/feed/:id`; evento → `/eventos/:id`; registro → `/mural/:id`;
  - banda (aceite, recusa, promoção, remoção, saída) → `/bandas/:slug`;
  - o convite não tem link: quem age é a seção de convites no topo da página.

## `/notificacoes`

- A seção **"Convites de banda"** continua no topo, vinda de `GET /api/band-invitations/mine`. É onde se aceita e recusa, e assim um convite nunca fica perdido na página 2.
- Na lista, o item do convite mostra só o estado: "Aguardando sua resposta", "Aceito", "Recusado", "Cancelado" ou "Expirou".
- Abas "Todas / Não lidas" (`unreadOnly`, voltam para a página 1); paginação de 20.
- **Item**:
  - avatar do ator (ou da banda), com o ícone do tipo no canto; sem ator, como no lembrete de amanhã, o ícone ocupa o lugar do avatar;
  - a frase, o trecho do post entre aspas e o tempo relativo de `lastActivityAt`;
  - não lida tem ponto e fundo `primary/[0.04]`.
- **Ações no item**:
  - clicar no corpo marca como lida e navega para o alvo;
  - à direita, "marcar como lida" e excluir (com confirmação).
- "Marcar todas como lidas" usa o `unreadCount` do provider.

## `/minha-conta`

Card "Notificações" com o checkbox "Receber convite de banda por e-mail". Mudar chama `PUT /api/profile/notification-settings` e faz `replaceUser` com o `ProfileDto` novo.

## Roteiro de verificação

Com a API e o Vite no ar. O script `notifications-ui.mjs` vive fora do repositório.

1. **Push com duas abas (1440 px).** A curte um post de C, e o sino de C sobe sem recarregar, com um toast "Admin curtiu seu post". "Ver" leva ao post e a notificação fica lida.
2. **Agrupamento.** B também curte, e a lista de C mostra "Admin e mais 1 pessoa curtiram seu post".
3. **Celular (390 px).** O sino aparece no topo. `/notificacoes` mostra o convite no topo e o item com "Aguardando sua resposta". Aceitar leva à banda e zera o contador.
4. **Queda de conexão.** Com a API parada, nenhum toast de erro aparece. Ao voltar, o sino se atualiza sozinho.
5. **Preferência.** Desmarcar o e-mail de convite em `/minha-conta` persiste ao recarregar.
