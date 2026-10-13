---
paths:
  - "src/pages/**/services/mocks.ts"
  - "src/api/mock-delay.ts"
  - "src/api/mock-fixtures.ts"
  - "src/pages/feed/**"
  - "src/pages/videos/**"
  - "src/pages/fotos/**"
  - "src/pages/eventos/**"
  - "src/pages/perfil/**"
  - "src/pages/notificacoes/**"
  - "src/pages/admin/**"
  - "src/pages/reportar-problema/**"
---

# Decisões — Mocks por página

Nesta entrega o backend tem Auth, Perfil e mídia, e Bandas e convites. Todas as outras telas existem para validar design e navegação e para **propor o contrato** que cada módulo vai cumprir. O mock é andaime: quando o endpoint real nasce, o `mocks.ts` morre no mesmo commit.

## A forma: service real com implementação fake

- Cada página tem `services/<secao>-service.ts` com as funções e os DTOs **que a tela precisa**, assinatura igual à que o `base-service` vai ter (`getFeed(query: PagedRequest): Promise<PagedResult<PostDto>>`).
- A implementação delega para `services/mocks.ts`: `export const getFeed = mocks.getFeed`. Ligar o backend = trocar a linha por `apiGet<PagedResult<PostDto>>('/api/posts', query)` e apagar o `mocks.ts`. Hook, componentes e i18n não mudam.
- `mocks.ts` guarda fixtures tipadas e simula o servidor: pagina com `mockPage(items, query)` de `src/api/mock-delay.ts`, espera `delay(350)` (para o loading aparecer), e **muta a fixture** em ações (like, marcar lida) para a tela refletir o refetch — nunca a tela edita o array local (regra do boilerplate: ação → service → `refresh()`).
- Enum vem como **número** do backend, então o mock já entrega número e a página converte com `const` + `Record` exaustivo (`perfil/genres.ts`, `perfil/cities.ts`, `videos/moderation-status.ts`, `notificacoes/notification-type.ts`, `eventos/event-status.ts`, `bandas/band-member-role.ts`, `bandas/band-invitation-status.ts`). ⚠️ O enum mora em arquivo próprio, nunca no `*-service.ts`: o service importa o `mocks.ts`, e o `mocks.ts` importando o enum do service fecha um ciclo que estoura em `Cannot access X before initialization`.
- **Mock nunca cruza de página pela tela.** Feed precisa de eventos para o rail? Importa `getUpcomingEvents` do service de `eventos/`. Entre `mocks.ts` a importação de fixtures compartilhadas (`people`, `bandSummaries`, `avatar()`, `picture()` de `src/api/mock-fixtures.ts`, que ficou sem dona quando o perfil virou real; `videos`/`photos` na fila de moderação) é permitida: é assim que aprovar na moderação faz o vídeo aparecer na galeria. (service nunca é promovido, mas pode ser importado do caminho da dona).
- Imagem de mock vem de `https://picsum.photos/seed/<slug>/<w>/<h>` e avatar de `https://api.dicebear.com/9.x/thumbs/svg?seed=<nickname>`; sem binário no repositório. Em produção o backend entrega URL assinada do storage.
- Dado de exemplo tem cara de ABC Paulista (bandas, casas de show e cidades verossímeis) para o design ser avaliado com conteúdo real, mas nenhum nome de pessoa ou banda real.

## O que apagar ao ligar cada módulo

| Tela | Módulo (ordem em `api/.claude/rules/dominio.md`) | Some |
|---|---|---|
| `/feed`, `/feed/:id` | 3 · Feed | `feed/services/mocks.ts` |
| `/eventos*` e o rail do feed | 4 · Eventos | `eventos/services/mocks.ts` |
| `/videos`, `/fotos`, `/admin/moderacao` | 5 · Galeria e moderação | `videos/`, `fotos/`, `admin/moderacao/services/mocks.ts` |
| `/notificacoes`, sino | 6 · Notificações | `notificacoes/services/mocks.ts` |
| `/reportar-problema` | 7 · Suporte | `reportar-problema/services/mocks.ts` |

~~Módulo 1 · Perfil e mídia~~ ligado em 13/10/2026: `perfil/services/mocks.ts` morreu e as fixtures foram para `src/api/mock-fixtures.ts`.

~~Módulo 2 · Bandas e convites~~ ligado em 13/10/2026: `bandas/services/mocks.ts` morreu e a fixture de convite saiu de `notificacoes/services/mocks.ts`, com o campo `invitation` e o `respondInvitation`. Os convites de `/notificacoes` vêm da API (`bandas.md`). `bandSummaries` continua em `mock-fixtures.ts` porque feed e eventos ainda marcam bandas de exemplo.

Quando o último `mocks.ts` morrer, `src/api/mock-delay.ts` e `src/api/mock-fixtures.ts` morrem juntos e esta rule vira histórico.

## Roteiro de verificação

1. `grep -rln "mocks" src/pages/*/services/*-service.ts` lista só as telas da tabela acima; nenhuma tela de auth.
2. Em cada tela mockada: o skeleton aparece por ~350 ms; paginar troca os itens; uma ação (like, marcar lida, aprovar) muda o dado depois do refetch.
3. `grep -rn "picsum\|dicebear" src --include=*.ts --include=*.tsx -l` lista só os `mocks.ts` e `src/api/mock-fixtures.ts`.
