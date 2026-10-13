---
paths:
  - "src/theme/**"
  - "src/components/**"
  - "src/layout/**"
  - "src/index.css"
  - "index.html"
---

# Decisões — Design system

Escrita antes da primeira tela. O Noise Vault é um site público de comunidade, não um painel: o visual precisa ter identidade (metal, cartaz de show, escuro) sem sacrificar legibilidade em celular barato à noite. Toda decisão visual nova entra aqui **e** em `src/theme/tokens.css`, nunca inline numa página.

## Escuro por padrão, claro por escolha

O tema padrão é **escuro**. `index.html` aplica a classe `dark` no `<html>` antes do primeiro paint lendo `app-color-scheme` do `localStorage` (`dark` | `light` | `auto`; ausente = `dark`). O `user-menu` é o único lugar que troca o tema (`theme/color-scheme.ts`). Mexeu no default? Mude o script do `<head>` também, senão a página pisca.

Alternativa descartada: só escuro. Perfil e galeria de fotos em tela clara ao sol são casos reais; o custo de manter o claro é só disciplina de token.

## Tokens: `tokens.css` é o único lugar de cor

🔴 **Nenhuma cor é escrita fora de `src/theme/tokens.css`.** Hex, `oklch()`, `rgb()` ou nome de cor em página, componente ou `layout/` é bug de revisão. `grep -rnE "#[0-9a-fA-F]{3,8}\b|oklch\(|rgb\(" src --include=*.tsx --include=*.ts --include=*.css | grep -v src/theme/` deve voltar vazio. Os componentes `ui/*` gerados pelo shadcn consomem os tokens por nome (`bg-primary`, `text-muted-foreground`) e também não carregam cor.

Paleta (valores em `tokens.css`, aqui só a intenção):

- **`--primary`**: o vermelho da marca, herdado do `#D32F2F` do legado e levemente dessaturado para não vibrar sobre fundo escuro. Aparece **só** em ação primária, link, anel de foco, item ativo da navegação e like ativo. Vermelho em fundo, borda de card ou título é bug: a marca funciona porque é rara.
- **Neutros quentes**: fundo, card, borda e texto no escuro puxam levemente para o marrom do `#3C140C` do legado (matiz ~25 em oklch, croma baixíssimo). Nada de cinza azulado de dashboard. No claro, neutros quase brancos com o mesmo matiz.
- **`--highlight`**: âmbar discreto, só para o selo de banda verificada e destaques raros. Não é segunda cor de botão. (`--accent` continua sendo o hover neutro do shadcn, não a cor de destaque.)
- **`--scrim` / `--on-scrim`**: véu escuro e texto claro **sobre imagem** (capa do Celtic Frost no `/bem-vindo`, play sobre miniatura, legenda de foto, "+N" da grade de mídia). Fixos nos dois temas, porque a imagem não muda com o tema. `bg-black`, `text-white` e afins são bug: usar `bg-scrim/60`, `text-on-scrim`.
- **Semânticos**: `--success`, `--warning`, `--info`, `--destructive` para toast, badge e estado. `--destructive` é diferente de `--primary` de propósito: excluir não pode parecer "a ação principal".
- **`--ring`** = `--primary`. Foco visível sempre.

## Tipografia: cartaz de show, corpo limpo

- **Display**: `Oswald Variable` (`@fontsource-variable/oswald`), **caixa alta** com `tracking-wide`, em todo título de página e de seção (`h1` 28px/32px em `md`, `h2` 22px, `h3` 18px) e no wordmark. Condensada e pesada lê como cartaz de show sem virar fonte "gótica" ilegível. Alternativa descartada: Bebas Neue, que só existe em caixa alta e não serve para título longo de evento.
- **Corpo**: `Inter Variable` 15px (14px abaixo de `md`), metadados 13px em `text-muted-foreground`, `tabular-nums` em contador e data.
- Fontes auto-hospedadas pelo fontsource, importadas uma vez em `main.tsx`. **Sem CDN.**
- `fontSize` inline numa página é bug: a escala é `text-sm/base/lg` + as classes de título em `global.css` (`.app-h1`, `.app-h2`, `.app-h3`).

## Imagens do projeto

`src/assets/` guarda só imagem que é **conteúdo fixo do produto**, vinda do legado: `celtic-frost.jpg` (capa do *To Mega Therion*, fundo do `/bem-vindo`) e `caio.jpg` (foto do autor na página Sobre). Imagem de usuário nunca entra no repositório: vem do backend. A `/bem-vindo` força `.dark` no próprio contêiner porque o texto vai sobre a capa em qualquer tema.

## Espaço, raio, elevação

- Escala de espaço: 4 / 8 / 12 / 16 / 24 / 32 (`gap-1 … gap-8`). Nada fora disso.
- Raio: `--radius: 0.5rem` (8px). Card 12px (`rounded-xl`), pílula/badge/avatar 999px.
- Elevação por **borda**, não por sombra: no escuro, sombra some; `border-border` separa card do fundo. Sombra só em popover e dialog (o shadcn já faz).
- Animação: `.app-page-enter` (160 ms, opacity + 4px de translate) na raiz de cada página, e as do shadcn em dialog/sheet. Nenhuma outra.

## Referências (Mobbin, outubro/2026) e o que foi tirado de cada uma

| Superfície | Referência | O que vale aqui |
|---|---|---|
| Auth | [Spotify web login](https://mobbin.com/screens/3e71d4a6-a301-40f8-9364-f21d9c80fe8b), [Deezer web login](https://mobbin.com/screens/1981de16-2f8e-407d-be79-a7c1da2defe3) | card de 400px centralizado em fundo escuro, logo acima, campos empilhados, **um** botão primário largo, link de alternância ("Não tem conta? Criar conta") no rodapé do card |
| Reset de senha | [Fabric](https://mobbin.com/flows/02565cf2-471a-4a18-973e-25565e4fc98b), [Supabase](https://mobbin.com/flows/0c0e68d7-bfa9-44d4-8dec-1cb0427d106c) | "esqueci" pede só o e-mail e responde com estado "confira sua caixa" sem revelar se existe; "redefinir" pede senha + confirmação; sucesso volta para o login |
| Shell | [Circle feed](https://mobbin.com/screens/da165274-6203-4aac-992a-04e17695335b), [Reddit home](https://mobbin.com/screens/b0a34206-6866-4f0f-bc42-02fd0bfefced) | sidebar de 240px com seções, topbar com busca + sino + avatar, coluna central de até 680px, rail direito de 320px só em `xl` |
| Feed | Reddit, [Contra community](https://mobbin.com/screens/b9a282d8-0c50-496e-896b-d4b20840e925) | card de post: avatar, nome, `@nickname`, tempo relativo, texto, grid de mídia, ações like/comentar; nada de sombra |
| Galerias | grid de 1/2/3 colunas | miniatura com proporção fixa, título no hover em desktop e sempre visível em mobile |
| Eventos | card com banner 16:9, data em pílula sobreposta, gêneros como badges | detalhe com banner largo, bloco de "quando/onde/quanto" em lista de fatos |
| Perfil | banner + avatar sobreposto + contadores + tabs | banda e pessoa partilham a moldura; banda acrescenta integrantes e selo |
| Notificações | lista com ponto de não lido; convite de banda com dois botões inline | nada em modal para ler notificação simples |

## Mobile primeiro

🔴 **Toda tela é conferida a 390px antes de 1440px.** Abaixo de `md` (768px): sidebar vira `bottom-nav` com 4 itens fixos (Feed, Vídeos, Eventos, Perfil — os `mobile: true` do `nav-config`) mais o botão "Mais", que abre um `Sheet` com o resto por seção; o rail direito some abaixo de `xl` (1280px); listas em grid viram 1 coluna; botões de ação ocupam a largura. Nenhum elemento com largura fixa acima de 360px. `useIsMobile()` (`hooks/use-media-query.ts`) é a única fonte de breakpoint em código; o resto é classe responsiva do Tailwind.

## Componentes: `ui/*` é primitivo, `App*` é o contrato

- `src/components/ui/*` é o que o CLI do shadcn gera. Não é importado por página nem por `layout/` **exceto** através dos componentes `App*`. `grep -rn "components/ui/button" src/pages src/layout` deve voltar vazio.
- **Botão é sempre `AppButton`** (`intent`: `primary` | `secondary` | `danger` | `neutral` | `ghost`; `loading` desabilita e mostra spinner). Só ícone: `AppIconButton` (exige `label`, que vira `aria-label` e tooltip).
- Campo de formulário é sempre `AppField` (react-hook-form + zod), senha é `AppPasswordField` (olho para mostrar), texto longo `AppTextareaField`, escolha única `AppSelectField` (`emptyLabel` adiciona a opção "não informar", que vira `''` no formulário) e múltipla escolha `AppChipsField` (chips com `aria-pressed`; `max` desabilita os não marcados quando o limite chega).
- Confirmação destrutiva via `useAppConfirm()` — recebe `onConfirm`, **não devolve booleano**.
- Avatar é `AppAvatar` (iniciais como fallback, `verified` para o selo). Badge é `AppBadge` (`tone`). Upload de avatar é `AppAvatarUploader` e de capa é `AppBannerEditor` (pessoa e banda; recebem callbacks, não conhecem service). Estado vazio é `AppEmptyState`. Carregando é `AppSkeleton` com preset. Cabeçalho de página é `AppPageHeader`. Paginação é `AppPagination`.
- Toast é só o do `base-service` (via `sonner`); página nenhuma chama `toast()` para erro de API.

## Roteiro de verificação

1. `npm run build` limpo; greps de cor, de `ui/button` fora de `app-button` e de acentuação fora de `i18n/` (exceto `api/client.ts`, `api/mock-fixtures.ts` e os `services/mocks.ts`, cujas fixtures são dado de exemplo) vazios.
2. Abrir `/entrar` a 390px e a 1440px: card centralizado, sem scroll horizontal, foco visível ao navegar por Tab.
3. Trocar o tema no `user-menu`: nada pisca ao recarregar; os dois temas mostram a mesma hierarquia.
4. Simular `prefers-reduced-motion`: a entrada de página não anima.
