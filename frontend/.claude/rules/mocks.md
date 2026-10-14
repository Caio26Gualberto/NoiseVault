---
paths:
  - "src/pages/**/services/mocks.ts"
  - "src/api/mock-delay.ts"
  - "src/pages/perfil/**"
  - "src/pages/reportar-problema/**"
---

# Decisões — Mocks por página

Desde 14/10/2026 o backend tem todos os módulos de `dominio.md`, e nenhuma tela é mock. Enquanto existiram, as telas mockadas serviam para validar design e navegação e para **propor o contrato** que cada módulo ia cumprir. O mock é andaime: quando o endpoint real nasce, o `mocks.ts` morre no mesmo commit.

## A forma: service real com implementação fake

- Cada página tem `services/<secao>-service.ts` com as funções e os DTOs **que a tela precisa**, assinatura igual à que o `base-service` vai ter (`getFeed(query: PagedRequest): Promise<PagedResult<PostDto>>`).
- A implementação delega para `services/mocks.ts`: `export const getFeed = mocks.getFeed`. Ligar o backend = trocar a linha por `apiGet<PagedResult<PostDto>>('/api/posts', query)` e apagar o `mocks.ts`. Hook, componentes e i18n não mudam.
- `mocks.ts` guarda fixtures tipadas e simula o servidor: pagina com `mockPage(items, query)` de `src/api/mock-delay.ts`, espera `delay(350)` (para o loading aparecer), e **muta a fixture** em ações (like, marcar lida) para a tela refletir o refetch — nunca a tela edita o array local (regra do boilerplate: ação → service → `refresh()`).
- Enum vem como **número** do backend, então o mock já entrega número e a página converte com `const` + `Record` exaustivo (`perfil/genres.ts`, `notificacoes/notification-kind.ts`, `eventos/event-status.ts`, `bandas/band-member-role.ts`, `bandas/band-invitation-status.ts`). ⚠️ O enum mora em arquivo próprio, nunca no `*-service.ts`: o service importa o `mocks.ts`, e o `mocks.ts` importando o enum do service fecha um ciclo que estoura em `Cannot access X before initialization`.
- **Mock nunca cruza de página pela tela.** Feed precisa de eventos para o rail? Importa `getUpcomingEvents` do service de `eventos/`. (Service nunca é promovido, mas pode ser importado do caminho da dona.) ~~Fixtures compartilhadas entre `mocks.ts`~~ morreram com `src/api/mock-fixtures.ts` no módulo 6.
- Imagem de mock vem de `https://picsum.photos/seed/<slug>/<w>/<h>` e avatar de `https://api.dicebear.com/9.x/thumbs/svg?seed=<nickname>`; sem binário no repositório. Em produção o backend entrega URL assinada do storage.
- Dado de exemplo tem cara de ABC Paulista (bandas, casas de show e cidades verossímeis) para o design ser avaliado com conteúdo real, mas nenhum nome de pessoa ou banda real.

## O que apagar ao ligar cada módulo

**Não sobrou nenhum.** ~~Módulo 7 · Suporte~~ ligado em 14/10/2026: `reportar-problema/services/mocks.ts` morreu, e com ele `src/api/mock-delay.ts`. O contrato do mock (`{ label, title, description, pageUrl }` → `{ protocol }`) não sobreviveu: o backend redesenhou o relato como ticket com tags, páginas, anexos e conversa (`suporte.md`). Esta rule fica como histórico e como molde, se um módulo novo nascer antes do backend.

~~Módulo 6 · Notificações~~ ligado em 14/10/2026: `notificacoes/services/mocks.ts` e `notification-type.ts` morreram, e com eles `src/api/mock-fixtures.ts`, que não tinha mais quem usasse (`notificacoes.md`). Só sobra o `mock-delay.ts`, para o reportar-problema.

~~Módulo 1 · Perfil e mídia~~ ligado em 13/10/2026: `perfil/services/mocks.ts` morreu e as fixtures foram para `src/api/mock-fixtures.ts`.

~~Módulo 2 · Bandas e convites~~ ligado em 13/10/2026: `bandas/services/mocks.ts` morreu e a fixture de convite saiu de `notificacoes/services/mocks.ts`, com o campo `invitation` e o `respondInvitation`. Os convites de `/notificacoes` vêm da API (`bandas.md`). `bandSummaries` continua em `mock-fixtures.ts` porque eventos ainda marca bandas de exemplo.

~~Módulo 5 · Galeria e moderação~~ **removido** em 13/10/2026: as telas `/videos`, `/fotos` e `/admin/moderacao` saíram, com os mocks, e o mural das antigas (`mural.md`) entrou ligado à API. `people` e `picture` de `mock-fixtures.ts` continuam só para as notificações.

~~Módulo 4 · Eventos~~ ligado em 13/10/2026: `eventos/services/mocks.ts` morreu, e com ele `bandSummaries` de `mock-fixtures.ts` (ninguém mais marca banda de exemplo). `people`, `avatar` e `picture` continuam para vídeos, fotos e notificações.

~~Módulo 3 · Feed~~ ligado em 13/10/2026: `feed/services/mocks.ts` morreu. As abas "Tudo/Bandas" saíram com ele (`feed.md`). O link `/feed/1` da notificação de exemplo continua no mock de notificações até o módulo 6.

Quando o último `mocks.ts` morrer, `src/api/mock-delay.ts` morre junto e esta rule vira histórico.

## Roteiro de verificação

1. `grep -rln "mocks" src/pages/*/services/*-service.ts` lista só as telas da tabela acima; nenhuma tela de auth.
2. Em cada tela mockada: o skeleton aparece por ~350 ms; paginar troca os itens; uma ação (like, marcar lida, aprovar) muda o dado depois do refetch.
3. `grep -rn "picsum\|dicebear" src --include=*.ts --include=*.tsx -l` lista só os `mocks.ts`.
