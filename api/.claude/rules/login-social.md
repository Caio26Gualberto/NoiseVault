---
paths:
  - "**/NoiseVault.Application/Auth/**"
  - "**/NoiseVault.Application/Interfaces/IExternalIdentityService.cs"
  - "**/NoiseVault.Infra.Data/ExternalLogin/**"
  - "**/NoiseVault.Infra.Data/Identity/AuthenticateService/**"
  - "**/NoiseVault.Domain/Enums/Auth/**"
  - "**/NoiseVault.Api/Controllers/AuthController.cs"
  - "**/NoiseVault.Api/Controllers/ProfileController.cs"
---

# Decisões — Entrar com Google ou Facebook

Item 6 do `IDEIAS.md`, implementado em 17/10/2026, com o site já em `https://noisevault.net`. O motivo é o público: senha nova e confirmação de e-mail fazem gente das antigas desistir no cadastro, e a comunidade vem do grupo do Facebook. Rule irmã: `../../frontend/.claude/rules/login-social.md`. O resto de auth (senha, sessão, cookie) continua em `auth-e-contas.md`.

## Sem Firebase, sem fluxo de redirecionamento no servidor

O front pega a prova de identidade direto com o provedor e a manda para a API, que confere sozinha:

- **Google**: o Google Identity Services devolve um **ID token** (JWT). A API valida assinatura pelas chaves públicas do Google (`https://www.googleapis.com/oauth2/v3/certs`, guardadas em memória por 6 h e rebuscadas quando aparece um `kid` desconhecido), `iss` (`accounts.google.com` ou `https://accounts.google.com`), `aud` igual ao `Auth:Google:ClientId` e validade. Sem pacote do Google: o `System.IdentityModel.Tokens.Jwt` que já assina o nosso JWT valida o deles.
- **Facebook**: o SDK do JavaScript devolve um **access token**. A API chama `debug_token` com o token do app (`{AppId}|{AppSecret}`) e só aceita `is_valid` com `app_id` igual ao nosso, e depois lê `/me?fields=id,name,email` com `appsecret_proof`. Graph API `v24.0`, fixa no código.

Os dois ficam atrás de `IExternalIdentityService` (`Application/Interfaces/`), implementado em `Infra.Data/ExternalLogin/` com `HttpClient` tipado. A chave do vínculo é o `sub` do Google e o `id` do Facebook, gravados na tabela `UserLogins` do Identity (`LoginProvider` = `Google` | `Facebook`). Não existe tabela nova: a `UserLogins` já existia e cai em cascata com o `ApplicationUser` quando o acesso é revogado.

**Configuração**: `Auth:Google:ClientId`, `Auth:Facebook:AppId` (públicos; o front tem cópia no `.env`) e `Auth:Facebook:AppSecret` (segredo, só por variável de ambiente). Provedor sem configuração responde `EXTERNAL_PROVIDER_DISABLED` e o boot avisa no console, como o Spotify.

## Um endpoint para entrar, e o "ticket" para o segundo passo

`POST /api/auth/external { provider, token }` (`provider`: `1` Google, `2` Facebook) confere o token e decide. A resposta é sempre `ExternalLoginResponseDto { outcome, tokens, ticket, name, email, suggestedNickname, hasPassword }`, com `outcome`:

| `outcome` | Code | Quando |
|---|---|---|
| `1` SignedIn | `LOGIN_SUCCESS` | o login do provedor já está vinculado, ou o Google vinculou sozinho (abaixo). Vem `tokens` e o cookie `nv_refresh` |
| `2` SignupRequired | `EXTERNAL_SIGNUP_REQUIRED` | ninguém tem esse login nem esse e-mail: falta escolher o apelido |
| `3` LinkRequired | `EXTERNAL_LINK_REQUIRED` | já existe conta com o e-mail, mas o vínculo precisa da senha dela |

O **ticket** é o que liga o primeiro passo ao segundo sem confiar no cliente: a identidade conferida (provedor, chave, nome, e-mail) protegida pelo Data Protection com validade de **15 minutos** (`ITimeLimitedDataProtector`, propósito próprio). O cliente não consegue ler nem forjar, e o segundo passo não chama o provedor de novo. Ticket vencido ou adulterado → `400 EXTERNAL_TICKET_INVALID`, e a tela manda entrar de novo.

- `POST /api/auth/external/register { ticket, name, nickname, email }` cria a conta e já entra (`EXTERNAL_ACCOUNT_CREATED` ou `EXTERNAL_ACCOUNT_CREATED_CONFIRM_EMAIL`, com `tokens` e cookie). `name` vem preenchido do provedor e a pessoa pode corrigir; `nickname` segue `NicknameRules` (`INVALID_NICKNAME`, `NICKNAME_TAKEN`). `suggestedNickname` sai do nome: sem acento, minúsculo, espaço vira ponto, e sufixo numérico se já existir.
- `POST /api/auth/external/link { ticket, password }` confere a senha da conta com o e-mail do ticket, vincula e entra (`LOGIN_SUCCESS`). Senha errada → `401 INVALID_CREDENTIALS`.

Token que o provedor recusa é `400 EXTERNAL_TOKEN_INVALID`, **não 401**: o "Conectar" de Minha conta roda com sessão, e um 401 faria o interceptor do front achar que o token de acesso venceu, renovar e repetir.

Envio repetido (duplo clique, aba duplicada) não quebra: se o login já foi vinculado entre um passo e outro, o segundo passo só entra.

## Ninguém confirma e-mail, e nada é enviado na conta nova

O provedor já provou que a pessoa é dona do e-mail. Conta criada com o e-mail do provedor nasce com `EmailConfirmed = true` e **não recebe e-mail nenhum** (a boas-vindas continua sendo só a tela do front). O login social **não** olha `EmailConfirmed`: quem entra é provado pelo provedor, não pela caixa de entrada.

O Google manda `email_verified`; e-mail com `false` é tratado como ausente. O Facebook não manda nada parecido: a Graph API só devolve o `email`. Confiamos nele **para criar conta nova**, nunca para vincular a uma conta que já existe (seção seguinte).

## Vincular a uma conta que já existe

🔴 **Só o Google vincula sozinho; o Facebook pede a senha.** Decisão do usuário em 17/10/2026. Vincular pelo e-mail entrega a conta inteira a quem controla aquele e-mail no provedor, e só o Google afirma que verificou.

- **Google, conta já confirmada**: vincula, entra e manda o **aviso de vínculo** (abaixo).
- 🔴 **Google, conta com senha que nunca foi confirmada**: alguém pode ter cadastrado o e-mail de outra pessoa com uma senha só dele, esperando a dona chegar ("pré-sequestro de conta"). O Google prova que a dona é quem está entrando agora. Então: vincula, **apaga a senha**, marca o e-mail como confirmado, troca o `SecurityStamp` e revoga os refresh tokens. Quem cadastrou perde o acesso, e a dona cria a senha dela em Minha conta se quiser. Sem e-mail: a conta, na prática, nasce agora.
- **Facebook**: responde `LinkRequired` com `hasPassword`. Com senha, a tela pede a senha e chama `external/link`; se a conta ainda não estava confirmada, o vínculo a confirma (a pessoa provou a senha e o e-mail no Facebook). Sem senha (conta que só entra pelo Google), a tela orienta a entrar pelo Google e conectar o Facebook em Minha conta.
- Conta desativada → `403 ACCOUNT_INACTIVE` em qualquer caminho.

## Facebook sem e-mail: pede o e-mail, manda confirmação e deixa entrar

Decisão do usuário em 17/10/2026 (opção "a"). Há conta de Facebook criada só com celular, e o público mais velho do grupo tem bastante.

- `SignupRequired` vem com `email` nulo, e o `external/register` exige `email` (`EXTERNAL_EMAIL_REQUIRED` vazio, `INVALID_EMAIL` inválido, `EMAIL_ALREADY_IN_USE` se já é de alguém).
- A conta nasce com esse e-mail e `EmailConfirmed = false`, recebe o **e-mail de confirmação** de sempre e **entra na hora** (`EXTERNAL_ACCOUNT_CREATED_CONFIRM_EMAIL`).
- Enquanto não confirma, o e-mail **não vale**: `forgot-password` não manda link (já era assim), o convite de banda não sai por e-mail (`SendInviteEmail` confere `IsEmailConfirmed`), o aviso de vínculo não sai, e o login por senha (se a pessoa criar uma) recusa com `EMAIL_NOT_CONFIRMED`.
- O mesmo vale para o Google com `email_verified = false`, que cai neste caminho.

⚠️ **Risco aceito: e-mail digitado de outra pessoa.** Quem entra pelo Facebook pode digitar o e-mail de alguém e nunca confirmar, e esse e-mail fica ocupado. É o mesmo risco que o cadastro com senha já tinha (conta pendente ocupa o e-mail). A dona do e-mail recupera do mesmo jeito: reenvia a confirmação, confirma, pede "esqueci a senha", entra e **desconecta o Facebook** em Minha conta (permitido porque agora há senha).

## Aviso de vínculo: um e-mail transacional, só quando alguém conecta um provedor

`SendExternalLoginLinkedAsync` ("O Facebook foi conectado à sua conta"): sai quando um provedor é ligado a uma conta que **já existia** (o vínculo automático do Google numa conta confirmada, o `external/link` e o "Conectar" de Minha conta), para a pessoa perceber se não foi ela. Botão para Minha conta, onde ela desconecta e troca a senha. **Não** sai na conta nova nem no pré-sequestro desarmado, e só vai para e-mail confirmado. Falha de envio vira `EXTERNAL_LOGIN_NOTICE_SEND_FAILED` (Warning) e não desfaz o vínculo (chamada externa depois do commit).

## Minha conta: ver, conectar, desconectar e criar senha

Endpoints de dono em `/api/profile`, no `AuthAppService`:

- `GET /api/profile/sign-in-methods` → `SignInMethodsDto { hasPassword, emailConfirmed, providers }` (`SIGN_IN_METHODS_FOUND`).
- `POST /api/profile/sign-in-methods { provider, token }` conecta. O e-mail do provedor não precisa bater: quem está logado já provou ser o dono. Login do provedor de outra conta → `409 EXTERNAL_LOGIN_IN_USE`; provedor já conectado nesta conta → `409 EXTERNAL_LOGIN_ALREADY_LINKED`; sucesso → `EXTERNAL_LOGIN_LINKED` e o aviso de vínculo.
- `DELETE /api/profile/sign-in-methods/{provider}` desconecta (`EXTERNAL_LOGIN_UNLINKED`). 🔴 **Nunca deixa a conta sem porta**: sem senha e com um só provedor → `400 LAST_SIGN_IN_METHOD`. Não conectado → `404 EXTERNAL_LOGIN_NOT_FOUND`.
- `POST /api/profile/create-password { newPassword }` cria a senha de quem só entrou por provedor (`PASSWORD_CREATED`, `AddPasswordAsync`); quem já tem → `400 PASSWORD_ALREADY_SET`. Não derruba as outras sessões (nada foi trocado). Quem já tem senha usa o `change-password` de sempre.

Esqueci a senha também funciona para conta sem senha com e-mail confirmado: o `ResetPasswordAsync` do Identity grava a senha mesmo sem hash anterior.

## Fora de escopo, de propósito

- **Foto do provedor como avatar**: o avatar passa pelo processamento de imagem e pelo S3 (`armazenamento.md`); importar do Google ou do Facebook é outro caso de uso.
- **Data Deletion Callback do Facebook**: o Facebook aceita uma página de instruções no lugar do callback. A página "Como apagar seus dados" (`/apagar-dados`, front) cumpre; a exclusão é feita pela equipe a pedido.
- **Trocar o e-mail digitado errado**: hoje só pelo suporte.

## Limites

| Política | Limite | Endpoint |
|---|---|---|
| `external-login` | 20/5min por IP | `auth/external`: cada chamada vai ao Google ou ao Facebook |
| `register` | 5/h por IP | `auth/external/register` (cria conta, como o cadastro) |
| `login` | 10/5min por IP | `auth/external/link` (tentativa de senha, como o login) |
| `sign-in-method` | 10/h por conta | conectar, desconectar e criar senha em Minha conta |

## Eventos de log (contexto `Auth`)

| Evento | Nível | Quando |
|---|---|---|
| `EXTERNAL_TOKEN_REJECTED` | Warning | o provedor recusou o token, ou ele é de outro app (`Provider`, motivo curto; nunca o token) |
| `EXTERNAL_PROVIDER_UNAVAILABLE` | Error | o Google ou o Facebook não respondeu; a resposta é `500 EXTERNAL_PROVIDER_UNAVAILABLE` |
| `USER_REGISTERED_EXTERNAL` | Info | conta criada por provedor (`UserId`, `Provider`, `EmailConfirmed`) |
| `EXTERNAL_LOGIN_LINKED` | Info | provedor ligado a conta existente (`UserId`, `Provider`, `Via`: `auto`, `password` ou `account`; `ClaimedUnconfirmed`) |
| `EXTERNAL_LOGIN_UNLINKED` | Info | provedor desligado em Minha conta |
| `PASSWORD_CREATED` | Info | senha criada por quem só tinha provedor |
| `EXTERNAL_LOGIN_NOTICE_SEND_FAILED` | Warning | aviso de vínculo não foi aceito pelo transporte |

Nenhum leva e-mail, token, ticket ou senha.

## Roteiro de verificação

Pré-requisito: `Auth:Google:ClientId`, `Auth:Facebook:AppId` e `Auth:Facebook:AppSecret` no `appsettings.Development.json` (fora do Git), com `http://localhost:5173` nas origens do app do Google e `localhost` nos domínios do app do Facebook (em modo desenvolvimento, só quem é testador do app entra). Tokens reais saem do front (`frontend/.claude/rules/login-social.md`).

1. Sem configuração: `POST /api/auth/external {provider:1, token:"x"}` → 400 `EXTERNAL_PROVIDER_DISABLED`; boot com o aviso no console. Com configuração e token inventado → 400 `EXTERNAL_TOKEN_INVALID` e `EXTERNAL_TOKEN_REJECTED` no log; `provider: 9` → 400 `EXTERNAL_PROVIDER_INVALID`.
2. Google com e-mail novo → `outcome: 2`, `ticket`, `name`, `email`, `suggestedNickname`; `external/register` com o ticket → `EXTERNAL_ACCOUNT_CREATED`, `tokens`, cookie `nv_refresh`, `USER_REGISTERED_EXTERNAL`, nada no Mailpit. Repetir o `external` → `outcome: 1` direto. Ticket alterado em um caractere → 400 `EXTERNAL_TICKET_INVALID`.
3. Conta com senha e confirmada (`caio@test.local`) e Google do mesmo e-mail → `outcome: 1`, `EXTERNAL_LOGIN_LINKED` com `Via: auto`, aviso de vínculo no Mailpit; a senha continua valendo.
4. Registrar com senha um e-mail do Google sem confirmar, entrar com o Google → `outcome: 1`, `ClaimedUnconfirmed: true`, sem e-mail; login com a senha antiga → 401 `INVALID_CREDENTIALS`; `GET /api/profile/sign-in-methods` → `hasPassword: false`, `emailConfirmed: true`.
5. Conta com senha e Facebook do mesmo e-mail → `outcome: 3`, `hasPassword: true`; `external/link` com senha errada → 401; certa → `LOGIN_SUCCESS`, `Via: password` e aviso no Mailpit.
6. Facebook sem e-mail (usuário de teste sem e-mail) → `outcome: 2` com `email: null`; `external/register` sem `email` → 400 `EXTERNAL_EMAIL_REQUIRED`; com e-mail → `EXTERNAL_ACCOUNT_CREATED_CONFIRM_EMAIL`, tokens, e-mail de confirmação no Mailpit; `forgot-password` desse e-mail → nada no Mailpit até confirmar.
7. Em Minha conta da conta do passo 2: `DELETE /api/profile/sign-in-methods/1` → 400 `LAST_SIGN_IN_METHOD`; `create-password` → `PASSWORD_CREATED`; de novo → 400 `PASSWORD_ALREADY_SET`; agora o `DELETE` → `EXTERNAL_LOGIN_UNLINKED`; `POST sign-in-methods` com o Google de outra conta → 409 `EXTERNAL_LOGIN_IN_USE`.
8. `DELETE /api/admin/users/{id}` de uma conta com Google → o mesmo Google volta a dar `outcome: 2` (o vínculo caiu com o `ApplicationUser`).
