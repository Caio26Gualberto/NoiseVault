---
paths:
  - "src/auth/external/**"
  - "src/components/app-social-sign-in/**"
  - "src/pages/entrar/**"
  - "src/pages/criar-conta/**"
  - "src/pages/minha-conta/**"
  - "src/pages/institucional/privacidade.tsx"
  - "src/pages/institucional/apagar-dados.tsx"
---

# Decisões — Entrar com Google ou Facebook

Rule irmã: `../../api/.claude/rules/login-social.md`. A regra é do backend (quem confirma e-mail, quem vincula sozinho, o ticket); aqui fica o que a tela faz. Sessão, cookie e gates continuam em `auth.md`.

## Os SDKs, carregados só onde há botão

- **Google**: Google Identity Services (`https://accounts.google.com/gsi/client`). O botão é o **renderizado pelo próprio Google** (`renderButton`), porque a marca exige e porque é ele que abre o popup e devolve o ID token (`credential`). Tema `filled_black` no escuro e `outline` no claro, formato retangular (como os campos e o "Entrar"), largura do contêiner (200 a 400 px, o limite do GIS). 🔴 O contêiner leva `color-scheme: light`: o botão é um `iframe`, e com a página em `color-scheme: dark` o Chrome pinta um fundo branco opaco atrás dele (visto em produção em 17/10/2026).
- **Facebook**: SDK do JavaScript (`https://connect.facebook.net/pt_BR/sdk.js`, Graph `v24.0`, a mesma da API). O botão é nosso (`AppButton` com a cor `facebook` do `tokens.css` e o "f" em SVG), e o `FB.login({ scope: 'public_profile,email' })` precisa rodar **dentro do clique**, senão o navegador bloqueia o popup. Por isso o SDK carrega na montagem e o botão fica desabilitado até ele estar pronto.
- `src/auth/external/` guarda os carregadores (um `<script>` por URL, uma vez por página) e os tipos mínimos das duas globais. Nenhum pacote npm de terceiro.
- `VITE_GOOGLE_CLIENT_ID` e `VITE_FACEBOOK_APP_ID` (públicos) ligam cada botão; vazio esconde o botão. Sem nenhum dos dois, a área inteira some e `/entrar` fica como antes.

## Facebook primeiro, e o navegador do app do Facebook

Quem vem do grupo abre o link dentro do app do Facebook (ou do Instagram). Ali o Google recusa o login (`disallowed_useragent`). Então:

- O botão do **Facebook vem primeiro**, nas duas telas.
- No navegador interno (`FBAN`, `FBAV`, `FB_IAB` ou `Instagram` no `userAgent`) o botão do Google **não aparece**; no lugar, uma linha: "Para entrar com Google, abra o site no navegador do celular".

## Onde aparece

- `/entrar` e `/criar-conta`: acima do formulário, com o divisor "ou com e-mail". O fluxo é o mesmo nas duas: o provedor decide se a pessoa entra, cria conta ou vincula.
- `/minha-conta`: o painel **Formas de entrar** (abaixo).
- A **volta para o Ensaio** (`?voltar=`) e o `state.from` atravessam os passos: o hook leva os dois no `state` da navegação até a sessão abrir (`use-finish-sign-in.ts`).

## Os três desfechos de `POST /api/auth/external`

`use-external-sign-in.ts` manda o token e olha o `outcome`:

| `outcome` | Tela |
|---|---|
| `1` entrou | `acceptToken` do `AuthProvider` (o mesmo caminho do login por senha) e segue para onde ia; conta nova cai no `/bem-vindo` pelo `RequireAuth` |
| `2` falta o apelido | `/entrar/apelido` com o ticket no `state` |
| `3` falta vincular | `/entrar/vincular` com o ticket no `state` |

O ticket vive **só no `state` da rota** (some ao recarregar, e vale 15 minutos na API). Sem ele, as duas telas mostram "Essa etapa expirou" com o botão de voltar para `/entrar`, e `EXTERNAL_TICKET_INVALID` também leva para lá.

### `/entrar/apelido`: escolher o apelido

Nome (preenchido com o do provedor, editável), **Apelido** (preenchido com a sugestão da API, com a mesma dica e validação do cadastro) e **E-mail só quando o provedor não mandou** (Facebook criado com celular), com a explicação "Vamos mandar um link para confirmar". Enviar cria a conta e já entra; o toast diz `EXTERNAL_ACCOUNT_CREATED` ou `EXTERNAL_ACCOUNT_CREATED_CONFIRM_EMAIL`.

### `/entrar/vincular`: já existe conta com esse e-mail

Mostra o e-mail e o provedor. Com senha (`hasPassword`): campo de senha, "Conectar e entrar" e o link de esqueci a senha. Sem senha (a conta só entra pelo Google): a explicação e o botão do Google ali mesmo; depois de entrar, conecta o Facebook em Minha conta.

## Minha conta: Formas de entrar

`GET /api/profile/sign-in-methods` na montagem da página (`hooks/use-sign-in-methods.ts`). O painel lista:

- **Google** e **Facebook** (só os ligados no `.env`, mais qualquer um já conectado): "Conectado" com **Desconectar** (confirmação pelo `useAppConfirm`) ou o botão do provedor para **Conectar**. `LAST_SIGN_IN_METHOD` chega como toast; a tela não tenta adivinhar antes.
- O painel **Senha** troca de forma: com senha, o "Alterar senha" de sempre; sem senha, **Criar senha** (nova + confirmação, `POST /api/profile/create-password`), que não pede a atual.
- No painel **Acesso**, e-mail não confirmado ganha o aviso "E-mail ainda não confirmado" com **Reenviar confirmação**. Só acontece com quem entrou pelo Facebook e digitou o e-mail.

## Páginas públicas exigidas pelos provedores

`/privacidade` (Política de privacidade) e `/apagar-dados` (Como apagar seus dados), públicas, sem login e sem redirecionar quem tem sessão, numa moldura larga (`wide` no `publicRoutes`). O Facebook exige a segunda para o app ir ao ar; o Google pede a primeira na tela de consentimento. Os links ficam no rodapé da moldura pública (login, cadastro, as próprias páginas). O texto está em `institucional.json` e descreve o que o produto faz de verdade: os dados que guarda, os provedores (AWS em São Paulo, Google, Facebook), o cookie `nv_refresh`, a LGPD, e a exclusão **feita pela equipe a pedido** (não há autoatendimento).

## Roteiro de verificação

Com a API e o Mailpit no ar, `VITE_GOOGLE_CLIENT_ID` e `VITE_FACEBOOK_APP_ID` no `.env` e as mesmas chaves na API (`api/.claude/rules/login-social.md`). No app do Facebook em modo desenvolvimento, só administradores e testadores entram.

1. `/entrar` mostra Facebook, Google e "ou com e-mail". Sem as duas variáveis → a tela de antes. Com o DevTools simulando o user agent do app do Facebook (`... FBAN/FBIOS ...`) → só o Facebook e a linha do navegador.
2. Google com uma conta nunca usada → `/entrar/apelido` com nome e apelido sugeridos → "Criar conta" → toast `EXTERNAL_ACCOUNT_CREATED` → `/bem-vindo`. Sair e entrar com o Google de novo → direto no `/feed`.
3. Recarregar em `/entrar/apelido` → "Essa etapa expirou" → volta para `/entrar`.
4. Conta criada com senha e o Facebook do mesmo e-mail → `/entrar/vincular`; senha errada → toast `INVALID_CREDENTIALS`; certa → `/feed` e o aviso de vínculo no Mailpit.
5. Facebook sem e-mail → `/entrar/apelido` com o campo de e-mail; enviar → toast `EXTERNAL_ACCOUNT_CREATED_CONFIRM_EMAIL`, sessão aberta; `/minha-conta` mostra "E-mail ainda não confirmado" e o reenviar manda outro link.
6. `/minha-conta` da conta do passo 2 → Formas de entrar com o Google conectado e o painel "Criar senha"; desconectar o Google → toast `LAST_SIGN_IN_METHOD`; criar a senha → `PASSWORD_CREATED` e o painel vira "Alterar senha"; desconectar agora funciona; conectar o Facebook → "Conectado".
7. `/entrar?voltar=<Ensaio>` com o Facebook → volta para o Ensaio, inclusive passando pelo `/entrar/apelido`.
8. `/privacidade` e `/apagar-dados` abrem sem sessão e com sessão, a 390 px e a 1440 px.
