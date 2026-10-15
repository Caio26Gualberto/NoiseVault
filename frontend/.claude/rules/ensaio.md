---
paths:
  - "src/ensaio/**"
  - "src/layout/ensaio/**"
  - "src/pages/ensaio/**"
  - "src/pages/feed/components/opening-embed.tsx"
  - "public/manifest-ensaio.webmanifest"
---

# Decisões — Ensaio (músicos e bandas) no frontend

Rule irmã: `../api/.claude/rules/ensaio.md`. A regra é do backend; a tela só reflete. Codes em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`, textos em `i18n/pt-BR/ensaio.json`.

O módulo 8a do `IDEIAS.md` é "um site à parte com a mesma marca" (pedido do usuário em 14/10/2026), servido em `ensaio.noisevault.net`.

## Visual: o "D · Flyer limpo"

Escolhido pelo usuário em 15/10/2026, entre seis propostas do canvas https://claude.ai/artifact/KenNKLhDbgPrCjtgnT1AmE. O que pesou: A (mural da loja de discos) e B (classificados de zine) agradaram, mas "fica muita informação e difícil de ler".

- **Fundo**: cortiça bem discreta (`ensaio-cork`).
- **Anúncios**: flyers de papel retos, com a fita no topo. A fita é o único elemento torto, com ângulo variando pelo índice.
- **Foto**: em retícula de xerox (`ensaio-halftone`, a imagem em escala de cinza com `mix-blend-multiply`). É a **foto do flyer** da ficha; sem ela, o avatar do perfil.
- **Pouca coisa por flyer**: o instrumento enorme (Oswald), o nome, e "cidade · som". O resto fica na ficha ou no detalhe da vaga.
- **A tirinha "✂ …"** embaixo do flyer é a ação, na Special Elite:
  - "Chamar pra banda" na vitrine;
  - "Tenho interesse" nas vagas;
  - "N interessados" para quem é Owner.
- **Filtros**: os chips grandes dos instrumentos mais comuns (Vocal, Guitarra, Baixo, Bateria, Teclado) e o "Mais filtros". Este abre um `Sheet` com todos os instrumentos, a cidade (com "Todo o ABC"), o som e, na vitrine, "Só quem tem gravação".
- **Ficha e detalhe da vaga**: uma folha de papel (`PaperSheet`) com as seções separadas por tracejado (`PaperSection`).

## Tokens só dentro de `.ensaio`

- `tokens.css` ganhou um bloco `.ensaio` com `--paper`, `--paper-foreground`, `--paper-muted`, `--paper-rule`, `--tape`, `--cork`, `--cork-dot`, `--halftone`, `--halftone-dot` e `--chip-border`, expostos como `bg-paper`, `text-paper-muted`, `border-paper-rule`, `bg-tape`… no `@theme inline`.
  - Fora de um contêiner `.ensaio` essas cores não existem. É de propósito: o papel é do Ensaio, não do Noise Vault.
  - O card da vaga no feed (`opening-embed.tsx`) se embrulha num `div.ensaio` para usar o papel.
- `ensaio-cork` e `ensaio-halftone` são `@utility` no `global.css`.
- **Fonte de máquina de escrever**: `@fontsource/special-elite` (auto-hospedada, só o latin 400), em `font-typewriter`. Só na tirinha, no "Procura-se" e no selo da ficha. Texto corrido continua Inter, porque a máquina de escrever cansa a leitura.
- **A moldura é sempre escura** (`dark ensaio` no contêiner do shell, e no `<html>` quando o endereço é o subdomínio). O tema claro do user-menu não vale aqui.

## A moldura pelo endereço: um app, um build

- **`src/ensaio/frame.ts`** decide tudo:
  - `isEnsaioHost`: o `hostname` começa com `ensaio.`;
  - `ensaioPath(path)`: link **de dentro** do Ensaio. No subdomínio é o próprio caminho; no site principal leva o prefixo `/ensaio`;
  - `ensaioUrl(path)`: link do site principal **para** o Ensaio. Com `VITE_ENSAIO_URL` (produção: `https://ensaio.noisevault.net`) é absoluto; sem ele (dev), é `/ensaio/...`;
  - `mainUrl(path)`: link do Ensaio para o site principal, por `VITE_MAIN_URL` ou trocando o `ensaio.` do endereço.
- **`app.tsx`**:
  - no subdomínio, renderiza só `EnsaioRoutes` na raiz;
  - no site principal, monta as mesmas `EnsaioRoutes` em `/ensaio/*`.
  
  Em dev, `localhost:5173/ensaio` usa a sessão de sempre.
- **Rotas** (`layout/ensaio/ensaio-routes.tsx`), relativas: `/`, `musicos/:nickname`, `vagas`, `vagas/nova`, `vagas/:id`, `vagas/:id/editar`, `minha-ficha`. A navegação do Ensaio é `layout/ensaio/ensaio-nav.ts`; o `nav-config.tsx` não conhece essas telas, porque a moldura e o menu são outros.
- **`EnsaioLink`** (`src/ensaio/ensaio-link.tsx`) é o link do site principal para o Ensaio: vira `<a>` quando o endereço é absoluto e `<Link>` quando não é. Repassa as props, porque o `DropdownMenuItem asChild` e o `AppButton asChild` injetam `role`, `className` e eventos.
- **Notificação com link absoluto**: o item do sino e o "Ver" do toast usam `<a>` ou `window.location` quando o link do kind (o 28 leva a `ensaioUrl('/vagas/:id')`) é de outro endereço.
- **Boot no subdomínio** (`src/ensaio/boot.ts`):
  - põe `dark ensaio` no `<html>`, para os portais (dialog, sheet, toast) também pegarem;
  - troca o `document.title` e a `meta` do iPhone;
  - troca o `<link rel="manifest">` para `public/manifest-ensaio.webmanifest` ("Ensaio · Noise Vault", `start_url: /`). O Ensaio instalado no celular é outro app, com o mesmo ícone.

## Sessão e boas-vindas

- **`RequireAuth skipWelcome`** envolve o shell do Ensaio. A tela de boas-vindas é do site principal, e o `localStorage` dela é por endereço: sem o `skipWelcome`, a primeira visita ao subdomínio mandaria a pessoa para um `/bem-vindo` que não existe ali.
- **Sem sessão no subdomínio**: vai para `mainUrl('/entrar?voltar=<endereço atual>')`. O login (`use-login.ts`) e o `RedirectIfAuthenticated` voltam para o `voltar` **só se a origem for a do Ensaio** (`safeReturnUrl`); qualquer outra é ignorada, para não virar redirecionamento aberto.
- **Em produção**, a sessão atravessa os subdomínios pelo cookie em `.noisevault.net` (`auth.md`).
- **Em dev, `ensaio.localhost:5173` não compartilha a sessão.** A API em `localhost` e o front em `ensaio.localhost` são sites diferentes para o `SameSite=Strict`, e o cookie não vai. Esse endereço serve para ver a moldura na raiz e o redirecionamento para o login; o fluxo inteiro se testa em `localhost:5173/ensaio`.

## O cabeçalho do Ensaio tem o próprio menu da conta

Exceção registrada ao "`user-menu.tsx` é o único dono de tema e Sair" (`shell-e-navegacao.md`): os links do `user-menu` são relativos ao site principal e quebrariam no subdomínio. O menu do avatar do Ensaio tem só:
- "Voltar ao Noise Vault" e "Minha conta", pelo `mainUrl`;
- "Sair", que chama o mesmo `logout` do `AuthProvider` e manda para o login do site principal.

Não tem tema: a moldura é sempre escura.

## Telas

| Caminho (no subdomínio) | O que mostra |
|---|---|
| `/` | vitrine "Procura-se banda": filtros, flyers em 3 colunas (1 no celular, com a foto do lado), "Você não está na vitrine" quando a ficha não existe ou está desligada |
| `/musicos/:nickname` | a ficha em papel: instrumentos e nível, o que procura, a grade de disponibilidade, a estrutura, os links e as influências; "Chamar pra banda" (para quem é Owner), "Editar minha ficha" (para o dono), "Ver perfil no Noise Vault" |
| `/vagas` | as vagas abertas em flyers; "Abrir vaga" para quem é Owner de alguma banda |
| `/vagas/:id` | a vaga em papel, com status, descrição, objetivo e validade, e a tirinha "Tenho interesse" (que abre o dialog com o recado) ou "Interesse enviado" (com "Retirar interesse" e a dica de convidado ou dispensado). Para os Owners: o menu "…" (Editar, Renovar, Marcar como preenchida, Encerrar, Apagar) e a lista de interessados com Convidar e Dispensar |
| `/vagas/nova`, `/vagas/:id/editar` | o formulário: banda (só as que a pessoa é Owner, `GET /api/bands/owned`), instrumento, "Qual?" quando é "Outro", objetivo e texto |
| `/minha-ficha` | o editor da ficha: a **foto do flyer** no topo (`FlyerPhotoUploader`: escolher, prévia já em retícula, salvar, trocar e tirar; só depois de a ficha ser salva uma vez), o interruptor "Estou procurando banda" no topo, instrumentos (até 5, com nível, subir e tirar), objetivo, compromisso, alcance, grade de disponibilidade, estrutura, sons (chips, até 10), links (até 4) e influências |

- O próprio flyer não mostra "Chamar pra banda".
- **"Chamar pra banda"** abre o `InviteDialog`: escolher a banda e o instrumento (já preenchido com o principal da ficha). Ele manda o convite de banda de sempre (`inviteBandMember`).
- **Convidar um interessado** manda o instrumento da vaga, já traduzido, porque o `BandMember.instrument` é texto.
- **Listas**: `hooks/use-ensaio-list.ts` serve à vitrine e às vagas. Os filtros voltam para a página 1 a cada troca (`changeFilters`), com 12 por página.
- **Enums**: `ensaio-status.ts`, com `const` e `Record` exaustivos para instrumento, nível, objetivo, compromisso, alcance, dia, turno e os status da vaga e do interesse. `useInstrumentLabel()` dá o nome, ou o nome livre de "Outro".

## No site principal

- **Perfil**: o selo "Procurando banda" (`isLookingForBand`) leva à ficha no Ensaio.
- **Banda**: a seção "Vagas abertas" (`pages/ensaio/components/band-openings.tsx`, até 5, só abertas e não vencidas). Para o Owner, "Abrir vaga", que leva ao formulário no Ensaio. A seção some quando não há vaga e quem vê não é Owner.
- **Feed**: post com `opening` mostra "abriu uma vaga" no meta e o flyer em papel (`OpeningEmbed`). O card mostra o status e só tem a tirinha "Tenho interesse" com a vaga aberta.
- **Sino**: o kind 28, "**Fulano** tem interesse na vaga de baixo da **Banda**", leva à vaga.
- **Menu do usuário**: "Ensaio: músicos e bandas".

## Armadilhas

- **Tabela dentro de caixa com rolagem horizontal precisa de `relative`.** O `Checkbox` do Radix desenha um `input` escondido com posição absoluta. Sem um ancestral posicionado dentro da caixa, ele escapa do recorte e alarga a página no celular. Visto na grade de disponibilidade da ficha em 15/10/2026.
- **`fieldset` não encolhe sozinho** (`min-inline-size: min-content`). Por isso os do formulário da ficha levam `min-w-0`.

## Roteiro de verificação

Com a API, o Mailpit e o Vite no ar. O script `ensaio-ui.mjs` (Playwright + Brave, scratchpad da sessão que criou o módulo) cobre os passos abaixo; em 15/10/2026 passaram as 26 checagens.

1. **Moldura.** Uma conta nova abre `localhost:5173/ensaio`. Aparecem o cabeçalho do Ensaio (sem a sidebar do Noise Vault) e o aviso "Você não está na vitrine".
2. **Ficha.**
   - "Montar minha ficha", escolher Baixo, marcar sábado à tarde e à noite e "Tem equipamento".
   - Um link de outro site → erro no campo. O link do YouTube → "Ficha salva.".
   - "Ver como as bandas veem" mostra a folha com a grade.
3. **Vitrine.** O admin vê o flyer. O chip Baixo traz a pessoa, Bateria não traz, e "Mais filtros" → "Todo o ABC" mantém.
4. **Vaga.** O admin abre a vaga pelo formulário (banda, Baixo, texto) e cai no detalhe.
5. **Interesse.** A conta nova clica na tirinha, manda o recado → toast, e a tirinha vira "Interesse enviado".
6. **Convite.** O admin vê o interessado com o recado e convida → "Convite enviado." e o status "Convidado".
7. **Site principal.**
   - O selo no perfil leva à ficha, e a banda mostra "Vagas abertas".
   - O feed traz "abriu uma vaga".
   - O sino traz "tem interesse na vaga de baixo" e leva à vaga; o menu do usuário tem o Ensaio.
8. **390 px.** A vitrine, as vagas e a ficha não têm rolagem horizontal, e a barra de baixo tem as 3 seções.
9. **Subdomínio.** `ensaio.localhost:5173` sem sessão leva a `localhost:5173/entrar?voltar=http://ensaio.localhost:5173/`.
