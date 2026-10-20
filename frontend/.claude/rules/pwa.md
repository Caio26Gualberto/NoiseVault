---
paths:
  - "pwa/**"
  - "public/manifest.webmanifest"
  - "public/offline.html"
  - "public/icons/**"
  - "src/pwa/**"
  - "src/layout/install-app-dialog.tsx"
---

# Decisões — Instalável no celular (PWA)

Item 7 do `IDEIAS.md`, parte 1 (14/10/2026): o site vira app na tela inicial, com ícone, tela cheia, abertura rápida e uma página de "sem conexão". Para o público das antigas, que não tem costume de abrir site, o ícone ao lado do WhatsApp pesa. **Notificação no celular (Web Push) é outra etapa**, com backend próprio; está no `IDEIAS.md`.

## Manifesto e ícones

- `public/manifest.webmanifest`:
  - nome "Noise Vault", `start_url: /feed`, `scope: /`, `display: standalone`, `orientation: portrait`;
  - fundo e tema `#1c1515`, o mesmo do `theme-color` do `index.html`.
- **Ícone novo**: barra inclinada vermelha + "NV" ("N" claro, "V" vermelho), o desenho do wordmark.
  - O `favicon.svg` antigo desenhava um "H" do legado e foi trocado pelo mesmo ícone.
  - As fontes ficam em `public/icons/icon.svg` (cantos arredondados) e `icon-maskable.svg` (fundo inteiro, arte a 80% para caber no círculo que o Android recorta).
  - Os PNG (192, 512, maskable 512 e o `apple-touch-icon` de 180) são gerados a partir dos SVG; não há script no repositório. Mudou o SVG, gera de novo.
- **Ensaio**: no subdomínio `ensaio.`, o boot troca o `<link rel="manifest">` para `public/manifest-ensaio.webmanifest` ("Ensaio · Noise Vault", `start_url: /`), e ele vira outro app instalável, com o mesmo ícone (`ensaio.md`).
- O `index.html` liga o manifesto, o `apple-touch-icon`, as `meta` `apple-mobile-web-app-*` e a `description`.

## Service worker: só o esqueleto, nunca a API

- **Sem `vite-plugin-pwa`**: o projeto está no Vite 8, e o plugin pede uma versão anterior. O worker é pequeno e fica no repositório:
  - `pwa/service-worker.js` é o modelo;
  - `pwa/vite-plugin-service-worker.ts` gera o `dist/sw.js` no build, com a versão (o horário do build) e a lista de precache;
  - sem build não existe `sw.js`, e o registro só acontece em produção (`import.meta.env.PROD`): em dev, um worker atrapalharia o HMR.
- **Precache**: `offline.html`, o manifesto, os ícones, o JS e o CSS de entrada e as fontes latinas (Inter e Oswald). Uns 1,3 MB.
  - Ficam de fora os pedaços carregados sob demanda (hls.js, SignalR: `isDynamicEntry`), as fontes de outros alfabetos e as imagens da boas-vindas.
- **Fetch**:
  - só `GET` da mesma origem. A API, o S3 e o CloudFront da mídia **nunca** passam pelo worker: feed, mural e notificação precisam vir frescos, e vídeo em cache só ocuparia espaço;
  - **navegação** vai à rede e, se falhar, responde o `offline.html`. O `index.html` não vai para o cache: abrir a SPA sem rede faria o boot da sessão falhar contra a API.
  - **Arquivo do precache** responde do cache. Os nomes têm hash, então o cache nunca serve versão velha.
- **Versão nova**:
  - cada build gera um `sw.js` diferente, e o navegador o encontra ao abrir, de hora em hora e quando a aba volta a ficar visível;
  - o worker novo espera, e o app mostra o toast "Nova versão do Noise Vault — Atualizar". A ação manda `skip-waiting` e recarrega quando o controle troca;
  - os caches das versões antigas são apagados no `activate`.
  - ⚠️ O recarregamento só acontece depois de clicar em "Atualizar". Na primeira instalação, o `clients.claim()` também dispara `controllerchange`, e recarregar ali tirava a pessoa da página no meio do primeiro acesso.
  - O toast sai de `src/pwa/register-service-worker.ts`, fora de página e fora do `base-service`: é a segunda exceção à regra "página não chama `toast()`", junto do push de notificação. É aviso de infraestrutura, não erro de API.

## A página sem conexão

`public/offline.html` é HTML estático, com estilo inline e texto em português.
- **Exceção registrada** a duas regras: "cor só em `tokens.css`" e "texto só no i18n". A página precisa abrir sem o JS do app; as cores são as do `theme-color`/ícone.
- "Tentar de novo" recarrega.

## "Instalar o app"

- O item fica no `user-menu.tsx` (o dono de tema e "Sair"), acima de "Sair":
  - **Android e Chrome no computador**: aparece quando o navegador dispara `beforeinstallprompt` (guardado logo no `main.tsx`, por `listenInstallPrompt`). Clicar abre o pedido nativo de instalação;
  - **iPhone e iPad (Safari)**: a Apple não deixa o site pedir. O item abre o `InstallAppDialog`, com o passo a passo "Compartilhar → Adicionar à Tela de Início";
  - **já instalado** (`display-mode: standalone` ou `navigator.standalone`) ou sem suporte: o item não aparece.
- O estado sai de `src/pwa/install-prompt.ts` (`useInstallMode`, com `useSyncExternalStore`).

## Produção

- O `sw.js` **não pode ir para cache longo** (`Cache-Control: no-cache`), senão a versão nova demora a chegar. O manifesto vai com `application/manifest+json`. Os dois cabeçalhos são gravados no objeto do S3 pelo `.github/workflows/deploy-front.yml` (`../api/.claude/rules/hospedagem.md`, "O bucket e o deploy").
- Service worker exige HTTPS; em dev, o `localhost` vale.

## Roteiro de verificação

`npm run build` e `npx vite preview --port 4173`. O script `pwa-check.mjs` (Playwright + Brave) cobre:
1. **Manifesto** responde como `application/manifest+json`, com `standalone`, `/feed` e as cores; os três ícones respondem 200; o `apple-touch-icon` está no `head`.
2. **Worker**:
   - ativo no escopo `/`, e a página fica controlada depois de recarregar;
   - um cache `noisevault-shell-*` só, com o `offline.html` e o JS de entrada, e nada de `/api`.
3. **Sem conexão**: com o servidor parado, navegar para `/feed` mostra a página "Sem conexão", com o ícone vindo do cache; "Tentar de novo", com o servidor de volta, abre o site.
4. **Versão nova**:
   - outro build, e `update()` → toast "Nova versão do Noise Vault";
   - "Atualizar" recarrega, e o cache troca de versão.
5. **Instalar**: no iPhone (emulado, em dev), o menu mostra "Instalar o app", que abre o passo a passo. No computador, sem o evento do navegador, o item não aparece.

Resultado em 14/10/2026: as 20 checagens passaram. Falta o teste num aparelho de verdade, porque o pedido nativo de instalação não abre em navegador automatizado.

## Tela cheia no iPhone: a área do relógio e do notch

- O `index.html` usa `apple-mobile-web-app-status-bar-style: black-translucent` com `viewport-fit=cover`: aberto pelo ícone, o app desenha por baixo da barra de status, sem faixa preta em cima.
- 🔴 O preço é que **todo elemento colado no topo precisa somar `env(safe-area-inset-top)`**. Sem isso, o relógio, a bateria e a ilha dinâmica ficavam por cima do wordmark e do avatar (relato de 19/10/2026, num iPhone em produção).
- Quem soma hoje:
  - a `TopBar` (altura `3.5rem + inset`, com o inset de `padding-top`) e o cabeçalho do Ensaio;
  - o `PublicFrame` e a boas-vindas (no `padding` de cima e de baixo);
  - os botões do visualizador de mídia (`top` com o inset);
  - o `SheetContent` de lado e de cima (`padding-top`) e o seu botão de fechar.
- No navegador comum e no Android o inset é 0, e nada muda. Embaixo, a `BottomNav` já somava o `safe-area-inset-bottom`.
- Elemento novo `fixed`/`sticky` no topo segue o mesmo desenho.
