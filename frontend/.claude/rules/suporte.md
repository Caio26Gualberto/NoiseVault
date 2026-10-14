---
paths:
  - "src/pages/reportar-problema/**"
  - "src/pages/suporte/**"
  - "src/pages/admin/suporte/**"
  - "src/layout/last-location.ts"
---

# Decisões — Suporte no frontend

Rule irmã: `../api/.claude/rules/suporte.md`. A regra é do backend; a tela só reflete. Codes em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`. Decisão do usuário em 14/10/2026: relato vira ticket no banco, visto numa tela de admin, sem GitHub; vídeo de até 2 min; seletor opcional de páginas.

## Telas

| Caminho | O que mostra | Quem |
|---|---|---|
| `/reportar-problema` | formulário: tipo (chips), resumo, descrição, páginas (chips, opcional), prints e 1 vídeo, aviso do contexto enviado | qualquer pessoa |
| `/suporte` | "Meus relatos": os próprios tickets, paginados, com status e "Respondido" | quem reportou |
| `/suporte/:id` | o ticket e a conversa com a equipe | quem reportou |
| `/admin/suporte` | caixa de entrada: abas por status com contagem, busca, filtros de tipo, página e prioridade, ordenação | `Support.View` |
| `/admin/suporte/:id` | ticket, conversa, triagem (status, prioridade, nota interna) e contexto do navegador | `Support.View` (triagem só com `Support.Edit`, resposta só com `Support.Reply`) |

- O item "Reportar problema" (Institucional) marca também `/suporte` e `/suporte/:id` (`subRoutes`). A seção **"Administração"** voltou à sidebar com "Suporte", atrás de `Support.View` no próprio item; a seção não tem permissão (`shell-e-navegacao.md`).
- Os caminhos ficam em `reportar-problema/support-paths.ts`, e os enums, rótulos, tons e o mapa caminho → página em `reportar-problema/support-enums.ts`. A dona dos DTOs e do service do usuário é `reportar-problema`; `suporte/` e `admin/suporte/` importam do caminho dela. Conversa, resumo e os hooks de detalhe e mensagens moram em `suporte/` e o admin importa de lá.

## Formulário

- **Pré-marcado, não obrigatório**: "Bug" e o aparelho pela largura da tela (`useIsMobile`: Celular abaixo de 768 px, senão Computador). A página de origem pré-marca a área (`areaFromPath`: `/feed`, `/eventos`, `/mural`, `/perfil`, `/bandas`, `/notificacoes`, `/minha-conta`). Tudo desmarcável.
- **Página de origem**: `layout/last-location.ts` guarda, num valor de módulo, o último `pathname` do shell que não é `/reportar-problema` (`useRememberLocation` no `AppShell`). Recarregar a página do formulário perde a origem: aceito, porque então ninguém sabe de onde a pessoa veio. O formulário lê uma vez ao montar.
- **Transparência**: o aviso tracejado no fim diz o que vai junto (o caminho, o navegador e o tamanho da tela). Dado do navegador nunca sai escondido.
- **Anexos** (`components/report-attachments.tsx`), como o composer do feed: o envio começa ao escolher o arquivo (`/api/support/media/photos` e multipart com `/api/support/media/videos`), até 6 arquivos e 1 vídeo de até 2 min e 200 MB (`supportVideoLimits`), checagem local antes. Remover aborta ou apaga a mídia. "Enviar relato" fica desabilitado com "Enviando anexos…" enquanto houver upload.
- **Sucesso** troca o formulário pelo protocolo e por "Acompanhar relato" (`/suporte/:id`). A resposta chega pelas notificações; nada de e-mail.

## Conversa

- `TicketConversation` (`suporte/components/`): bolhas com avatar, as de quem está vendo à direita, as da equipe com borda `primary/30` e o selo "Equipe". Mensagens mais antigas primeiro, de 100 em 100 ("Carregar mais" soma).
- Enviar chama o service e **refaz a busca** das mensagens e do ticket (a primeira resposta da equipe muda o status para "Em análise"; a resposta de quem reportou num resolvido reabre).
- Quem reportou: resolvido mostra a dica "responda para reabrir"; "Não procede" troca o campo pelo aviso de encerrado.
- Mídia em processamento consulta o ticket de novo a cada 5 s (`useTicketDetail` + `usePolling`), como o post.

## Caixa de entrada

- Abas por status com a contagem do `GET /api/admin/support/summary`, mais "Todos". A barra rola na horizontal no celular, em vez de quebrar linha.
- Cada linha: ponto de prioridade (`supportPriorityDot`), protocolo, status, "Alta" quando for, **"Aguardando você"** (`awaitingStaff`), tipos, quem reportou, tempo e contagem de prints, vídeo e mensagens.
- A lista e a contagem refazem a busca quando a `version` do `NotificationsProvider` sobe: ticket novo e resposta chegam como notificação, e a caixa se atualiza sozinha.
- Tons: Novo `info`, Em análise `warning`, Resolvido `success`, Não procede `neutral`. Vermelho só na prioridade alta: a marca não vira cor de status (`design-system.md`).
- **Triagem** (`components/triage-panel.tsx`) salva status, prioridade e nota juntos (`PUT`, toast de sucesso) e troca o ticket pelo DTO que volta. O painel remonta pela `key` quando o servidor muda esses campos (uma resposta que coloca em análise).
- **Contexto**: quem reportou (link para o perfil), "Veio de" (link para a rota), tela e navegador em fonte mono.

## Roteiro de verificação

Com a API, o ffmpeg e o Vite no ar. O script `support-ui.mjs` (Playwright + Brave) vive fora do repositório e registra uma conta nova pelo Mailpit.

1. **Formulário (1440).** Fã vai de `/eventos` para "Reportar problema": Bug, Computador e Eventos marcados; o aviso cita `/eventos`. Enviar vazio mostra o erro do resumo. Com resumo, descrição, Feed e um print, envia e mostra o protocolo. "Acompanhar relato" abre `/suporte/:id` com "Novo", e o item "Reportar problema" continua marcado.
2. **Caixa de entrada.** Admin abre "Suporte" em Administração: o ticket aparece em Novo com "Aguardando você"; a busca pelo protocolo deixa só ele. No detalhe, "Veio de /eventos" e "1440 × 900".
3. **Conversa.** Admin responde: vira "Em análise". O fã vê a resposta com o selo "Equipe" e a notificação "A equipe respondeu o seu relato", que leva a `/suporte/:id`.
4. **Resolver e reabrir.** Admin marca Resolvido com nota: toast "Relato atualizado.". O fã vê a dica de reabrir e não vê a nota; responde e o ticket volta a "Novo".
5. **Celular (390).** Pelo "Mais", o formulário pré-marca Celular e Feed. Caixa de entrada, detalhe e formulário sem rolagem horizontal. Fã em `/admin/suporte` cai no `/feed`.

Resultado em 14/10/2026: todos os passos passaram.
