---
paths:
  - "src/pages/ajuda/**"
  - "src/pages/suporte/**"
  - "src/pages/admin/suporte/**"
  - "src/layout/last-location.ts"
---

# Decisões — Suporte no frontend

Rule irmã: `../api/.claude/rules/suporte.md`. A regra é do backend; a tela só reflete. Codes em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`. Decisão do usuário em 14/10/2026: relato vira ticket no banco, visto numa tela de admin, sem GitHub; vídeo de até 2 min; seletor opcional de páginas. Em 27/10/2026, a pedido do usuário, bug e sugestão se separaram (leigo confundia os dois no mesmo formulário), cada um num passo a passo, e a página virou "Ajuda e sugestões" em `/ajuda`.

## Telas

| Caminho | O que mostra | Quem |
|---|---|---|
| `/ajuda` | "Ajuda e sugestões": dois cartões ("Algo deu errado" e "Tenho uma ideia"), cada um abrindo um stepper com perguntas guiadas, prints e 1 vídeo e o aviso do contexto enviado | qualquer pessoa |
| `/suporte` | "Meus relatos": os próprios tickets, paginados, com status e "Respondido" | quem reportou |
| `/suporte/:id` | o ticket e a conversa com a equipe | quem reportou |
| `/admin/suporte` | caixa de entrada: abas por status com contagem, busca, filtros de tipo, página e prioridade, ordenação | `Support.View` |
| `/admin/suporte/:id` | ticket, conversa, triagem (status, prioridade, nota interna), contexto do navegador e "Apagar" | `Support.View` (triagem só com `Support.Edit`, resposta só com `Support.Reply`, apagar só com `Support.Delete`) |

- O item "Ajuda e sugestões" (Institucional) marca também `/suporte` e `/suporte/:id` (`subRoutes`). A seção **"Administração"** voltou à sidebar com "Suporte", atrás de `Support.View` no próprio item; a seção não tem permissão (`shell-e-navegacao.md`).
- Os caminhos ficam em `ajuda/support-paths.ts`, e os enums, rótulos, tons e o mapa caminho → página em `ajuda/support-enums.ts`. A dona dos DTOs e do service do usuário é `ajuda`; `suporte/` e `admin/suporte/` importam do caminho dela. Conversa, resumo e os hooks de detalhe e mensagens moram em `suporte/` e o admin importa de lá.

## Formulário

- **Dois cartões antes de qualquer campo** (`components/report-kind-picker.tsx`): "Algo deu errado" (bug) e "Tenho uma ideia" (sugestão), com ícone e uma frase de exemplo. Escolher grava `?tipo=bug` ou `?tipo=sugestao` na URL: o Voltar do navegador volta aos cartões, e um link pode abrir direto um dos dois. Rejeitado: abas no topo, porque leigo não lê aba como "escolha primeiro".
- **Um passo por vez** (`AppStepper` + `components/report-wizard.tsx`). Os passos de cada tipo ficam em `components/report-steps.tsx`:
  - **Algo deu errado**: Onde (páginas e aparelho) → O que fez → O que esperava e o que aconteceu → Prints → Enviar (resumo, revisão e aviso do contexto).
  - **Tenho uma ideia**: A ideia (resumo e texto) → Por quê (motivo e parte do site, os dois opcionais) → Imagens → Enviar (revisão e aviso).
  - "Continuar" valida só os campos do passo (`form.trigger`), e Enter num input fora do último passo avança em vez de enviar.
  - **O último passo é revisão: só o clique em "Enviar" envia.** "Continuar" e "Enviar" ocupam o mesmo lugar e levam `key` diferentes. Sem isso o React reaproveitava o mesmo `<button>`: o React 19 aplica a troca de passo numa microtask, antes de o navegador decidir o que fazer com o clique, e o "Continuar" do penúltimo passo já era `type="submit"` quando o navegador olhava, enviando sem passar pela revisão (visto em 27/10/2026). Por segurança, o `onSubmit` do formulário fora do último passo só avança. A revisão tem "Editar" por linha, que volta ao passo. O título do passo recebe o foco a cada troca, para o leitor de tela anunciar.
  - **Todos os passos ficam montados**, e só o atual aparece: o `ReportAttachments` aborta os uploads ao desmontar, e trocar de passo não pode cancelar um vídeo subindo.
  - "Voltar" no primeiro passo leva aos cartões. Se algo já foi escrito ou anexado, confirma antes, porque o que foi preenchido se perde.
- **A API não mudou.** O tipo vira tag: bug manda `Bug` mais os aparelhos marcados; sugestão manda só `Suggestion`. O chip "Conta" saiu do formulário, porque as áreas "Minha conta" e "Entrar e cadastro" já dizem isso. A tag continua na API, nos tickets antigos e no filtro do admin.
- **Descrição composta** (`report-description.ts`): cada resposta vira uma seção em Markdown, com subtítulo `##` (`compose.*` do i18n), e a resposta opcional vazia não entra. Os limites por campo (bug 3 × 1.500; ideia 3.000 e motivo 1.500; mínimo de 10 nos obrigatórios) cabem nos 20–5.000 da API. O `TicketSummary` mostra a descrição com `AppMarkdown`, e o ticket antigo, em texto puro, aparece igual por causa do `remark-breaks`.
- **Pré-marcado, não obrigatório** (só no bug): o aparelho pela largura da tela (`useIsMobile`: Celular abaixo de 768 px, senão Computador) e a área pela página de origem (`areaFromPath`: `/feed`, `/eventos`, `/mural`, `/perfil`, `/bandas`, `/artistas`, `/minha-conta`). Tudo desmarcável. A sugestão não pré-marca área: a ideia raramente é sobre a última página aberta.
- **Página de origem**: `layout/last-location.ts` guarda, num valor de módulo, o último `pathname` do shell que não é `/ajuda` (`useRememberLocation` no `AppShell`). Recarregar a página do formulário perde a origem: aceito, porque então ninguém sabe de onde a pessoa veio. O formulário lê uma vez ao montar (`hooks/use-report-submit.ts`).
- **Transparência**: o aviso tracejado no passo de envio diz o que vai junto (o caminho, o navegador e o tamanho da tela). Dado do navegador nunca sai escondido.
- **Anexos** (`components/report-attachments.tsx`), como o composer do feed: o envio começa ao escolher o arquivo (`/api/support/media/photos` e multipart com `/api/support/media/videos`), até 6 arquivos e 1 vídeo de até 2 min e 200 MB (`supportVideoLimits`), checagem local antes. Remover aborta ou apaga a mídia. "Enviar relato" fica desabilitado com "Enviando anexos…" enquanto houver upload.
- **Sucesso** troca o fluxo pelo protocolo ("Relato enviado" ou "Sugestão enviada") e por "Acompanhar relato" (`/suporte/:id`). A resposta chega pelas notificações; nada de e-mail.

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
- **Apagar** (`Support.Delete`, `canDelete` do DTO): botão "Apagar" (`danger`) no cabeçalho do detalhe, com `useAppConfirm` que avisa que some tudo e sugere "Não procede" para só encerrar. Depois volta para a caixa de entrada. É exclusão de vez (`../api/.claude/rules/suporte.md`), pensada para limpar relato de teste.
- **Contexto**: quem reportou (link para o perfil), "Veio de" (link para a rota), tela e navegador em fonte mono.

## Roteiro de verificação

Com a API, o ffmpeg e o Vite no ar. O script `support-ui.mjs` (Playwright + Brave) vive fora do repositório e registra uma conta nova pelo Mailpit.

1. **Formulário (1440).** Fã vai de `/eventos` para "Ajuda e sugestões": aparecem os dois cartões. Em "Algo deu errado", Eventos e Computador vêm marcados. "Continuar" com o campo vazio mostra o erro do passo. Preenche até o fim com um print, e o aviso cita `/eventos`. Envia e vê o protocolo. "Acompanhar relato" abre `/suporte/:id` com "Novo", as tags Bug e Computador e a descrição com os subtítulos formatados, e o item "Ajuda e sugestões" continua marcado. Em "Tenho uma ideia", o ticket sai só com a tag Sugestão. O Voltar do navegador no meio do fluxo volta aos cartões.
2. **Caixa de entrada.** Admin abre "Suporte" em Administração: o ticket aparece em Novo com "Aguardando você"; a busca pelo protocolo deixa só ele. No detalhe, "Veio de /eventos" e "1440 × 900".
3. **Conversa.** Admin responde: vira "Em análise". O fã vê a resposta com o selo "Equipe" e a notificação "A equipe respondeu o seu relato", que leva a `/suporte/:id`.
4. **Resolver e reabrir.** Admin marca Resolvido com nota: toast "Relato atualizado.". O fã vê a dica de reabrir e não vê a nota; responde e o ticket volta a "Novo".
5. **Celular (390).** Pelo "Mais", os cartões aparecem empilhados, e "Algo deu errado" pré-marca Celular e Feed. O stepper aparece como "Passo 1 de 5" com a barra. Caixa de entrada, detalhe e formulário sem rolagem horizontal. Fã em `/admin/suporte` cai no `/feed`.

Resultado em 14/10/2026: todos os passos passaram. O passo 1 e o 5 foram reescritos em 27/10/2026 (cartões e stepper) e ainda não foram refeitos no navegador.
