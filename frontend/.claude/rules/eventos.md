---
paths:
  - "src/pages/eventos/**"
  - "src/pages/feed/components/event-embed.tsx"
---

# Decisões — Eventos no frontend

Rule irmã: `../api/.claude/rules/eventos.md`. A regra é do backend; a tela só reflete. Codes em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`.

## Telas

| Caminho | O que mostra |
|---|---|
| `/eventos` | abas **Próximos / Já rolou**, chip **Vou** (só os que marquei), busca (nome, local ou banda), gênero e cidade |
| `/eventos/:id` | banner, faixa "Cancelado" quando for o caso, organizador (pessoa ou banda), fatos, line-up, "Vou", "Ingressos", menu "…" para quem gerencia, comentários |
| `/eventos/novo`, `/eventos/:id/editar` | formulário com banner, "Publicar como", cidade "Outra", line-up, gêneros, preço "a partir de", lotação e link |
| `/bandas/:slug` | seção **Shows** (próximos ou já rolou), mais o contador "N shows marcados" no hero |
| `/feed` | card do evento dentro do post automático (`EventEmbed`) e o rail "Próximos eventos" (`getUpcomingEvents(3)`) |

- **Cidade**: `useEventPlace()` (`event-place.ts`) mostra o `cityName` quando a cidade é "Outra", e o rótulo da lista nos outros casos. Toda tela de evento usa esse hook, nunca o `cityLabel` direto.
- **Preço**: vazio é "Entrada franca"; com valor é "A partir de R$ X" (o show grande tem setores).
- **Cancelado e já rolou**: imagem dessaturada, título riscado quando cancelado, selo no card. O botão "Vou" fica desabilitado, mas quem já tinha marcado ainda consegue desmarcar. "Ingressos" some nos dois casos, e o comentário some no cancelado.
- **Gerenciar**: o menu "…" (Editar, Cancelar evento, Apagar) aparece só com `canManage` da API. Cancelar e apagar passam por `useAppConfirm`. A edição de um evento cancelado redireciona para o detalhe.

## Formulário

- **Banner**: é o `AppBannerEditor`.
  - Na **criação**, ele trabalha com o arquivo local: o "Salvar capa" só guarda o arquivo e a posição na tela, e o upload acontece depois do `POST /api/events`, com o id em mãos. Se o upload falhar, o evento já existe e o toast de erro avisa.
  - Na **edição**, cada ação vai direto à API, como a capa da banda.
- **Line-up** (`components/lineup-editor.tsx`): um campo só, que busca bandas do Noise Vault (`GET /api/bands/search`, debounce de 350 ms, 2+ caracteres) e oferece "Adicionar "X" como nome". Enter adiciona como nome. Cada linha mostra se é banda do Noise Vault ou nome livre, com ↑/↓ e remover. O limite é 30, igual à API.
- **Cidade "Outra cidade"** mostra o campo "Nome da cidade", obrigatório no zod como na API.
- **Datas**: o `datetime-local` vira ISO UTC no envio. O zod confere que o término vem depois do início; "no passado" e "no máximo 7 dias" ficam com a API, que devolve `EVENT_IN_PAST` / `INVALID_EVENT_DATES` pelo toast.
- **Acessibilidade**: os selects "Publicar como" e "Cidade" têm `aria-label`, porque o `Label` do shadcn não se liga ao trigger do Radix.

## Listas

- `hooks/use-eventos-page.ts` refaz a busca a cada filtro e volta para a página 1 em toda troca (`changeScope`, `changeInterested`…).
- `hooks/use-event-detail.ts` troca o evento pelo DTO que "Vou" e "Cancelar" devolvem (`replace`), como no feed.
- `components/band-events.tsx` é a seção "Shows" da banda, com 4 por página e as mesmas abas.

## Roteiro de verificação

Com a API e o Vite no ar. O script `events-ui.mjs` (Playwright + Brave) vive fora do repositório.

1. **Criar (1440px).** Uma integrante abre `/eventos/novo`:
   - sobe a capa;
   - escolhe "Serra Negra (banda)", "Outra cidade" e Curitiba;
   - monta o line-up: busca "serra" e escolhe a banda, digita "Kreator" + Enter, adiciona "Sepultura" pelo botão e sobe uma posição;
   - marca Doom e informa preço e link.
   Publicar leva ao detalhe com o toast "Evento publicado. Ele já aparece no feed.", "A partir de R$ 45,00", "Tork n Roll · Curitiba" e o line-up na ordem.
2. **Feed.** O primeiro post é da Serra Negra, com "publicou um evento" e o card do evento (pílula de data, título, data e local).
3. **Celular (390px).** Uma conta fã marca "Vou" ("1 vai") e comenta. Em `/eventos`, as abas e o chip "Vou" filtram certo.
4. **Cancelar.** A organizadora usa "…" → "Cancelar evento" → confirmar: aparece a faixa vermelha com a data, o título riscado e os botões desabilitados. O card do feed passa a mostrar "Cancelado".
5. **Banda.** `/bandas/serra-negra` mostra a seção Shows com o evento, e o hero com "N shows marcados".
