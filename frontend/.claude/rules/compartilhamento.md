---
paths:
  - "src/components/app-share-button/**"
  - "vite.config.ts"
---

# Decisões — Compartilhar no frontend

Rule irmã: `../api/.claude/rules/compartilhamento.md`. A prévia (foto e título no WhatsApp e no Facebook) sai da rota `/s/...` da API; a tela só gera o link certo.

## `AppShareButton`

- Recebe o caminho da página (`/mural/12`) e o título, e gera `{origem}/s{caminho}` (`shareUrlFor`).
- **Celular** (`pointer: coarse` e com `navigator.share`): abre o menu de compartilhar do sistema, com título e link. A pessoa escolhe o WhatsApp, o Facebook ou o grupo, e cancelar não mostra nada.
- **Computador**: copia o link e mostra o toast "Link copiado — Cole no grupo ou no WhatsApp: o link aparece com foto e título". Se a cópia falhar, o toast mostra o link para copiar à mão.
  - O toast sai do componente: é a terceira exceção à regra "página não chama `toast()`", junto do push de notificação e do aviso de versão nova. Não é erro de API, é a confirmação de uma ação que não passa pela API.
  - O `navigator.share` do Windows abriria a janela de compartilhar do sistema, estranha no computador. Por isso a escolha é pelo tipo de tela, não pela existência da função.
- Variações: ícone (`AppIconButton`) ou botão com "Compartilhar" (`label`).

## Onde aparece

| Tela | Como |
|---|---|
| card do post (feed, perfil, banda) | ícone no rodapé; o título é "Fulano no Noise Vault" ou o nome da banda |
| `/eventos/:id` | ícone ao lado do "Vou" |
| `/mural/:id` | ícone ao lado do "…", **só com o registro publicado** (envio pendente não tem prévia) |
| `/bandas/:slug` e `/perfil/:nickname` | botão "Compartilhar" no hero, junto dos links |

## Dev: proxy do `/s/`

O `vite.config.ts` repassa `^/s/` para a API (`VITE_API_BASE_URL`), como o rewrite do Amplify vai fazer em produção. A chave é uma regex com `^/s/` justamente para não pegar `/suporte`. O service worker do PWA não guarda a navegação (`pwa.md`), então o link sempre chega à rota.

## Roteiro de verificação

O script `share-ui.mjs` (Playwright + Brave), com o Vite e a API no ar:
1. **Computador (1440).** O compartilhar do primeiro post do feed mostra "Link copiado", e a área de transferência tem `http://localhost:5173/s/feed/{id}`. Em `/mural/1`, `/eventos/1`, `/perfil/admin` e numa banda, o link copiado é o da página com `/s` na frente.
2. **O link.** Abrir `/s/mural/1` logado redireciona para `/mural/1`, que abre normalmente.
3. **Celular (iPhone emulado).** O compartilhar chama o `navigator.share` com o título "… no Noise Vault" e o link `/s/feed/{id}`.

Resultado em 14/10/2026: as 8 checagens passaram.
