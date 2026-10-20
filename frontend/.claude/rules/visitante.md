---
paths:
  - "src/auth/use-require-login.ts"
  - "src/auth/home-redirect.tsx"
  - "src/layout/visitor-actions.tsx"
  - "src/components/app-sign-in-prompt/**"
  - "src/hooks/use-document-title.ts"
---

# Decisões — O visitante sem conta

Rule irmã: `../api/.claude/rules/seo.md` (quais leituras a API abre e o que ela esconde sem login). Decisão do usuário em 20/10/2026, junto do SEO: banda, agenda, evento, mural e registro ficam abertos para leitura, e quem chega pela busca vê **a página de verdade**, no mesmo shell, só leitura. Não existe uma "versão pública" separada para manter.

## Rotas abertas

- `public: true` no `nav-config` marca o que o visitante abre: os itens `/eventos` e `/mural` e as `hiddenRoutes` `/eventos/:id`, `/mural/:id` e `/bandas/:slug`. O `nav-config` continua sendo a fonte única.
- O `app.tsx` monta o `AppShell` com `<RequireAuth allowVisitor>`, e cada rota do shell leva `allowVisitor={route.public}`. Com `allowVisitor` e sem sessão, o `RequireAuth` renderiza a página; com sessão, valem os mesmos gates de sempre (senha inicial, boas-vindas, permissão, `auth.md`). Rota sem `public` continua mandando para `/entrar` com `state.from`.
- **`/` do visitante vai para `/eventos`** (`HomeRedirect`, `visitorHomePath` em `auth/routes.ts`). Com sessão, continua indo para o `/feed`.
- O 404 do shell aparece para o visitante dentro da moldura dele.

## O shell sem sessão

- `useIsVisitor()` (`auth/use-require-login.ts`) é `status === 'unauthenticated'`. Durante o boot (`unknown`) ninguém é visitante, então nada pisca.
- **Topbar**: no lugar da busca de pessoas, do sino e do avatar, "Entrar" e "Criar conta" (`VisitorActions`).
- **Sidebar e bottom-nav**: só os itens `public`. No rodapé da sidebar, "Entrar" e "Criar conta" empilhados; na bottom-nav, o "Mais" vira "Entrar".
- **`NotificationsProvider` só com sessão**: o visitante não abre SignalR nem faz polling.
- **Faixa no topo do conteúdo** (`VisitorBanner`): "Você está vendo o Noise Vault sem conta", com os dois botões.
- O "Entrar" leva o `state.from` da página atual, e o `RedirectIfAuthenticated` devolve a pessoa para lá depois do login.

## O que cada página faz sem sessão

| Página | Muda para o visitante |
|---|---|
| `/eventos` | sem o filtro "Vou"; o "Vou" do card leva ao login; "Criar evento" continua e cai no login |
| `/eventos/:id` | "Vou" leva ao login; o organizador pessoa aparece sem link (perfil é fechado); os comentários viram o aviso "Entre para ler e escrever os comentários", e o hook nem busca a lista |
| `/mural` | sem "Meus envios"; "Enviar para o mural" cai no login |
| `/mural/:id` | "Eu estava lá" e curtir levam ao login; no lugar dos avatares de quem estava lá, só "N pessoas estavam lá"; o autor sem link; sem marcar pessoas nas fotos; as histórias viram o aviso, e o hook não busca os comentários |
| `/bandas/:slug` | "Seguir" leva ao login; integrantes sem link para o perfil; sem "Vagas abertas" (o Ensaio pede login); as publicações viram o aviso "Entre para ver as publicações da banda" |

- **Nenhuma chamada a endpoint fechado sem sessão.** Um 401 passaria pelo `auth-interceptor`, que tentaria renovar a sessão à toa. Por isso o hook do detalhe recebe `withComments` e os componentes que só funcionam logados não montam.
- O aviso de login é o `AppSignInPrompt` (`components/app-sign-in-prompt`): ícone de cadeado, a frase e os dois botões. A ação "leva ao login" é o `requireLogin()` do mesmo hook.
- **As marcações das fotos e os nomes de quem estava lá não chegam**: a API manda vazio sem login (`seo.md`). A tela só não oferece o que não tem.

## Título da aba

`useDocumentTitle()` (`src/hooks/`) põe "Título · Noise Vault" na aba, nas cinco páginas abertas: agenda, evento, mural, registro e banda. Volta o título anterior ao sair. Não serve ao SEO (o robô recebe o HTML da API, `seo.md`); é para a aba e o histórico do navegador.

## Roteiro de verificação

Com a API e o Vite no ar, numa janela anônima:
1. `http://localhost:5173/` cai em `/eventos`, com a faixa do visitante, "Entrar" e "Criar conta" na topbar e só Mural e Eventos na navegação (a 390px, a bottom-nav mostra Mural, Eventos e Entrar).
2. Abrir um evento: "Vou" leva a `/entrar`; entrar volta para o evento, já com sessão e com os comentários.
3. Abrir um registro do mural: sem marcações nas fotos, "N pessoas estavam lá" sem nomes, as histórias pedem login.
4. Abrir uma banda: integrantes sem link, sem vagas, publicações pedindo login; "Seguir" leva ao login.
5. `/feed`, `/perfil/admin` e `/minha-conta` levam a `/entrar`.
6. O console e a aba Network não mostram nenhum 401 nem chamada ao hub de notificações.
7. Logado, as cinco páginas ficam como antes.
