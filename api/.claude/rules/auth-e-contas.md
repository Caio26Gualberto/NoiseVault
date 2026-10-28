---
paths:
  - "**/NoiseVault.Application/Auth/**"
  - "**/NoiseVault.Application/Users/**"
  - "**/NoiseVault.Application/Profiles/**"
  - "**/NoiseVault.Application/Authorizations/**"
  - "**/NoiseVault.Infra.Data/Identity/**"
  - "**/NoiseVault.Infra.Data/Context/Seeding/**"
  - "**/NoiseVault.Domain/Authorization/**"
  - "**/NoiseVault.Domain/Entities/User.cs"
  - "**/NoiseVault.Api/Controllers/AuthController.cs"
  - "**/NoiseVault.Api/Controllers/ProfileController.cs"
---

# Decisões — Auth e contas

Um único tipo de usuário (`User`, sempre uma pessoa — ver `dominio.md`) entra por três caminhos: **registro público** (vira `User`, precisa confirmar o e-mail antes do primeiro login), **Google ou Facebook** (vira `User` sem senha, com o e-mail do provedor já confirmado; decisões em `login-social.md`) e **convite de admin** (vira o papel escolhido, com senha temporária e e-mail já confirmado). O ciclo de permissões está no `CLAUDE.md`, que é convenção e não decisão.

## Registro: validar antes de escrever, e devolver o motivo

`RegisterUser` devolve `UserCreationResult` com um `EUserCreationFailure` classificado, e `AuthAppService` traduz em `PASSWORD_TOO_WEAK` / `EMAIL_ALREADY_IN_USE` / `INVALID_EMAIL`. **A validação roda antes de qualquer escrita** (`UserValidators` + `PasswordValidators` do próprio `UserManager`, mais `AnyAsync` no e-mail e no nickname): inserir o `User` e só então chamar `CreateAsync` faria e-mail repetido estourar no índice único e virar 500. Reusar os validators do Identity em vez de reimplementar a política é o que impede a mensagem de mentir quando a configuração mudar.

As três escritas do registro (`Users`, `UsersIdentity`, `UserRoles`) rodam dentro de um `IUnitOfWork` aberto pelo `AuthAppService`. O `AuthenticateService` não abre transação própria: quem orquestra o caso de uso é quem decide a fronteira do commit.

## Registro devolve "confira seu e-mail", não tokens

`POST /api/auth/register` responde `REGISTRATION_PENDING_CONFIRMATION` com `{ userId, email }` e **nenhum token**. `ApplicationUser.EmailConfirmed` nasce `false` no registro e `true` no convite e no seed — é a coluna do Identity, não uma nova. O login recusa conta não confirmada com `403 EMAIL_NOT_CONFIRMED`.

🔴 **O login confere a senha antes de olhar a confirmação.** A ordem é: usuário existe → senha bate → e-mail confirmado → conta ativa. Invertida, qualquer pessoa descobriria se um e-mail está cadastrado e pendente sem saber a senha. `INVALID_CREDENTIALS` para as duas primeiras falhas; `EMAIL_NOT_CONFIRMED` só para quem provou ter a senha.

**O e-mail de confirmação sai depois do commit**, e a falha de envio **não desfaz o registro**: vira `EMAIL_CONFIRMATION_SEND_FAILED` (Warning) e a resposta continua sucesso. É o oposto do convite (abaixo), de propósito: no convite a senha só existe no e-mail, então conta sem e-mail é conta inacessível; no registro a pessoa conhece a senha e existe `POST /api/auth/resend-confirmation`. Rollback aqui a obrigaria a preencher o formulário de novo por um problema de SMTP.

`resend-confirmation` e `forgot-password` respondem **200 sempre** (`CONFIRMATION_EMAIL_SENT`, `PASSWORD_RESET_EMAIL_SENT`), exista a conta ou não. Internamente só enviam quando faz sentido (conta existente e, no reenvio, ainda não confirmada; no reset, confirmada e ativa).

⚠️ **Risco aceito em 13/10/2026 — tempo de resposta.** O envio é síncrono após o commit; a resposta de um e-mail existente demora o tempo do SMTP a mais que a de um inexistente. Com Mailpit é imperceptível; com provedor real é um side-channel de enumeração. A correção é mover o envio para a fila `emails` (padrão do Buscaprocesso) quando o provedor de produção entrar — não antes, porque não há segundo caso real para a fila.

## Tokens de confirmação e de reset: do Identity, embrulhados para URL

`IAuthenticateService` gera e consome os dois tokens via `UserManager` (`GenerateEmailConfirmationTokenAsync`, `ConfirmEmailAsync`, `GeneratePasswordResetTokenAsync`, `ResetPasswordAsync`). O token do Identity tem `+`, `/` e `=`; a `Infra.Data` o entrega **já em base64url** (`WebEncoders`) e desfaz na volta. A `Application` nunca vê o token cru nem sabe que ele é do Identity.

- Confirmação vale **24 h** (`DataProtectionTokenProviderOptions`, provider padrão).
- Reset de senha vale **1 h** por um provider próprio (`PasswordResetTokenProvider`, registrado em `options.Tokens.PasswordResetTokenProvider`). O default do Identity é 24 h para tudo; um link de reset vivo por um dia é tempo demais para quem teve a caixa de entrada vazada.
- Os dois tokens são de uso único por construção: mudam com o `SecurityStamp`, e `ConfirmEmailAsync`/`ResetPasswordAsync` o atualizam.

🔴 **Data Protection em container é efêmero.** Os tokens são protegidos pelas chaves do Data Protection; em dev no Windows elas persistem em `%LOCALAPPDATA%\ASP.NET\DataProtection-Keys`. Num container sem volume, reiniciar a API invalida **todo** link pendente de confirmação e de reset. ~~Antes do primeiro deploy em container: `PersistKeysToDbContext` ou filesystem montado.~~ Feito em 17/10/2026: `AddDataProtection().SetApplicationName("noisevault").PersistKeysToDbContext<AppDbContext>()` no `AddInfra`, tabela `DataProtectionKeys` (o `AppDbContext` implementa `IDataProtectionKeyContext`). Em dev também passa a usar o banco: derrubar o volume do Postgres invalida os links pendentes, o que é o esperado.

## Redefinir senha: provar posse do e-mail não é abrir sessão

`POST /api/auth/reset-password { email, token, newPassword }` responde `PASSWORD_RESET_SUCCESS` **sem tokens**; o front manda para `/entrar`. O link pode ter sido aberto em outro aparelho, e emitir sessão a partir de um e-mail transforma a caixa de entrada em credencial completa. No sucesso **todos os refresh tokens do usuário são revogados** — quem trocou a senha porque desconfiou de alguém precisa que esse alguém caia.

Token inválido ou vencido → `400 PASSWORD_RESET_INVALID`; senha fraca → `400 PASSWORD_TOO_WEAK` (a mesma política do registro, pelos validators do Identity). O `email` vai na URL do link porque o Identity precisa identificar a conta e `userId` sequencial revelaria enumeração; é a única exceção à regra "dado pessoal nunca em URL" do monorepo, registrada em `email.md`.

## Nickname é identidade pública

`User.Nickname` é o handle da URL `/perfil/:nickname`, pedido **no registro** (introduzir depois exigiria gerar handles para contas antigas). Regras em `NicknameRules`: minúsculo, `^[a-z0-9](?:[a-z0-9._]{1,28}[a-z0-9])?$` (3 a 30 caracteres, sem começar ou terminar em `.`/`_`), normalizado com `Trim().ToLowerInvariant()`, e uma lista reservada (`admin`, `noisevault`, `feed`, `eventos`, `videos`, `fotos`, `bandas`, `artistas`, `perfil`, `api`, `suporte`) para não colidir com rotas do site. `INVALID_NICKNAME` para formato; `NICKNAME_TAKEN` (409) para reservado ou em uso.

- O índice único de `Nickname` **não filtra soft delete**, pelo mesmo motivo do e-mail: handle é identidade e não pode ter duas linhas vivas. Na revogação de acesso, nickname e e-mail recebem o tombstone `deleted+{id}+{original}`.
- `Nickname` usa a **colação default** (não a `case_insensitive` do e-mail) porque já é gravado em minúsculas e porque a colação não determinística do Postgres recusa `LIKE`. É por isso que a busca da equipe (`GET /api/admin/users?search=`) procura em `Name` e `Nickname`, nunca em `Email` com `Contains`.

## Tela de boas-vindas não é estado do servidor

A tela de boas-vindas do primeiro acesso (`/bem-vindo` no front) **não tem nada na API**. Até 13/10/2026 existia `User.OnboardingCompletedAt` + `POST /api/profile/complete-onboarding`; foi removido (migration `RemoveOnboardingCompletedAt`) porque gravar no banco que alguém já viu um texto de apresentação é custo sem dono: nenhuma regra de negócio, segurança ou relatório depende disso. O front marca no `localStorage` por usuário (`frontend/.claude/rules/auth.md`). Consequência aceita: quem troca de navegador ou limpa os dados vê a apresentação de novo. Se um dia o onboarding coletar dado de perfil (cidade, gêneros), o dado vai para o módulo de Perfil — não volta uma flag de "já viu".

## Refresh token: atômico, com janela de reuso

`ConsumeRefreshTokenAsync` marca o token como consumido com `ExecuteUpdateAsync ... WHERE RevokedAt IS NULL`; zero linhas afetadas é recusa (duas abas renovando ao mesmo tempo: uma ganha, a outra toma `INVALID_TOKEN` e tenta de novo com o cookie novo que o navegador já recebeu). Um token **já revogado** apresentado de novo depois de 30 s (`RefreshReuseGrace`) é tratado como roubo: **todos** os refresh tokens daquele usuário caem. Dentro dos 30 s é a corrida legítima de abas e nada acontece. Herdado do Buscaprocesso.

`Logout` e troca de senha revogam todos os refresh tokens por `ExecuteUpdateAsync`, não por `SELECT` + loop.

## Refresh token num cookie `HttpOnly`, token de acesso no corpo

Decidido em 14/10/2026, antes dos subdomínios (`IDEIAS.md`, item 8; `hospedagem.md`). Até então o par ia no corpo e o front guardava os dois no `localStorage`, que é preso a cada endereço (o login de `noisevault.net` não valeria em `ensaio.noisevault.net`) e fica ao alcance de qualquer script injetado na página.

- **O refresh token nunca aparece no JSON.** `TokensDto.RefreshToken` é `[JsonIgnore]`: continua circulando entre `AuthAppService` e controller, e o controller o grava no cookie. O corpo leva só `token` (o de acesso, 15 min), que o front guarda em memória.
- **O cookie** (`Api/Authentication/RefreshCookie.cs`): nome `nv_refresh`, `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth` (só o refresh e o logout o recebem, nunca o resto da API), validade igual à do token (`AuthTokenRules.RefreshTokenLifetime`, 7 dias). `Domain` vem de `Auth:RefreshCookie:Domain`: vazio em dev (vale para `localhost`, e cookie não distingue porta), `.noisevault.net` em produção, para todos os subdomínios.
- **Quem grava**: `login`, `refresh-token` (o cookie novo da rotação), `profile/change-password` e `profile/set-initial-password`. **Quem apaga**: só o `logout`. 🔴 **Refresh recusado não apaga o cookie**: com várias abas, a resposta de falha da aba que perdeu a corrida chegaria depois da vencedora e apagaria o cookie novo dela, derrubando a sessão inteira (visto em 14/10/2026). Um cookie inválido que sobra não abre nada e é trocado no próximo login.
- `refresh-token` não tem corpo. Sem cookie, a resposta é a mesma de token inválido (`INVALID_TOKEN`).

### CSRF: `SameSite=Strict` mais a checagem de origem

Cookie vai sozinho em toda requisição para a API, inclusive as que um site de fora dispara. Duas barreiras no `refresh-token` e no `logout`:

1. `SameSite=Strict`: o navegador não manda o cookie em requisição iniciada por outro site. Os subdomínios são o **mesmo site** (`noisevault.net`), então passam.
2. `OriginGuard` (`Api/Authentication/OriginGuard.cs`): com `Origin` presente e fora de `Cors:AllowedOrigins`, a resposta é `403 ORIGIN_NOT_ALLOWED` antes de ler o cookie. Sem `Origin` (cliente que não é navegador), passa: o `SameSite` não se aplica a ele, e sem cookie roubado não há o que forjar.

O resto da API não precisa da checagem: ela é autorizada pelo `Authorization: Bearer`, que nenhum site de fora consegue montar. O CORS já era por lista exata e ganhou `AllowCredentials()`, que exige isso.

## Troca de senha: `ChangePasswordAsync`, nunca `Remove` + `Add`

`change-password` usa `UserManager.ChangePasswordAsync(user, current, new)`. A versão `RemovePasswordAsync` + `AddPasswordAsync` do boilerplate deixava a conta **sem senha** quando a nova era recusada pela política. `set-initial-password` (só com `password_change_required`) continua trocando sem a atual, e os dois devolvem `TokensDto` porque o claim precisa sair do access token.

⚠️ **Regra de senha vive em dois lugares e precisa concordar.** A política está no `AddIdentity` (6 caracteres, um dígito, sem maiúscula/símbolo obrigatórios). O validador do frontend espelha — senão o usuário toma erro do servidor num campo que a tela deu por válido.

## Entrada na equipe é por convite, e a senha nunca é digitada por outra pessoa

`POST /api/admin/users/invite` (`Authorization.Users.Invite`, que **não** entra em `RoleDefaultPermissions` — só Admin) cria um usuário com papel e permissões adicionais opcionais, e-mail já confirmado e nickname derivado do e-mail (parte antes do `@`, normalizada, com sufixo numérico se colidir). Não existe endpoint em que alguém digita a senha do colega.

- **O papel vem por id, não por nome**: papéis são dinâmicos. Papel `Admin` ignora permissões adicionais — as dele são implícitas.
- A senha sai do `TemporaryPasswordGenerator` e **existe apenas em memória e no e-mail** — nunca no `AuthorizationAuditLog`.
- 🔴 **O e-mail de convite é enviado dentro da transação, antes do commit.** Falha de envio ⇒ rollback + `INVITE_EMAIL_FAILED`: uma conta cuja senha ninguém conhece é pior que um convite não enviado. É a única operação em que uma chamada externa fica dentro da transação, e é deliberado. Com `ConsoleEmailSender`, a senha temporária sai no log (`EMAIL_STUBBED` com o assunto; o corpo não é logado — em dev use o Mailpit).

## Primeiro login por convite: bloqueio no servidor, não só na tela

Quem entra por convite nasce com `IsNeededChangePassword = true`, que vira o claim `password_change_required` no JWT. O `PasswordChangeRequiredFilter` (filtro global) devolve `403 PASSWORD_CHANGE_REQUIRED` em **qualquer** rota enquanto o claim existir, exceto as mínimas para trocar a senha: `profile`, `profile/change-password`, `profile/set-initial-password`, `admin/authorization/me`, `auth/logout`, `auth/refresh-token`.

## Revogar acesso não apaga histórico

`DELETE /api/admin/users/{id}` **não apaga a linha**: `User` é referenciado com `Restrict` por tudo que registra autor. A operação tem dois lados na mesma transação:

- **Domínio**: soft delete de `Users` com `IsActive = false` e tombstone em `Email` e `Nickname`.
- **Identity**: purga de verdade — `UserClaims`, `UserRoles`, `RefreshTokens` e o `ApplicationUser` (`RoleManagementService.RevokeUserAccessAsync`). É isso que faz o acesso acabar no ato.

Duas guardas: `CANNOT_DELETE_SELF` e `LAST_ADMIN_PROTECTED`.

## Lista de usuários do admin: quem está se cadastrando

Pedido do usuário em 27/10/2026, depois do anúncio do site: ver quantas pessoas se cadastram e quem são. `GET /api/admin/users` e `GET /api/admin/users/summary` ficam atrás de `Authorization.Users.View` (só o Admin hoje). A tela é a `/admin/usuarios` (`../frontend/.claude/rules/usuarios-admin.md`).

- **Só conta ativa.** A lista e o resumo filtram `IsActive`. Conta revogada tem tombstone no e-mail e no nickname, não é cadastro de ninguém e só polui a contagem.
- **A linha** traz nome, nickname, e-mail, avatar, cidade (`City.Name`, senão o `CityName` livre), data do cadastro e três campos do Identity: `emailConfirmed`, `hasPassword` e `providers` (`EExternalProvider`). Eles mostram se o login social está sendo usado e quem parou antes de confirmar o e-mail.
- **Identity em lote, sem N+1.** `IAuthenticateService.GetSignInMethodsAsync(userIds)` faz duas consultas para a página inteira, uma em `ApplicationUser` e uma em `UserLogins`. O método de um usuário só continua servindo a "Minha conta".
- **Resumo** (`USER_SIGNUP_SUMMARY_FOUND`): `today`, `last7Days`, `last30Days`, `total` e `unconfirmed`. "Hoje" é o dia em `America/Sao_Paulo` (`AppTime.TodayStartUtc`), porque meia-noite UTC é 21h aqui e o anúncio da noite cairia no dia seguinte. 7 e 30 dias contam para trás a partir de agora. `unconfirmed` conta os `ApplicationUser` sem `EmailConfirmed`. Conta revogada não entra, porque o `ApplicationUser` dela foi purgado.
- **E-mail** só nessa resposta, que já exige a permissão. A busca continua em `Name` e `Nickname`, nunca no e-mail (a colação, acima). Nada disso vai para log ou URL.

## Papéis e permissões: Admin é implícito, o resto é claim

O primeiro uso de negócio das permissões do catálogo é o mural das antigas: `Archive.Create`, `Archive.Edit`, `Archive.Delete` e `Archive.Review` (a fila de envios da comunidade; grupo "Mural das antigas"). Nenhum papel padrão as recebe; quem publica é o `Admin` ou um papel criado pelo painel (`mural.md`). O suporte veio depois, com `Support.View`, `Support.Edit`, `Support.Reply` e `Support.Delete` (grupo "Suporte", `suporte.md`), e trouxe `IPermissionChecker.UsersGrantedAsync`: a lista de quem tem uma permissão (papel `Admin`, claim de papel ou claim de usuário), que a notificação de ticket novo usa para achar a equipe. A verificação de banda (15/10/2026) acrescentou `Bands.Verifications.View` e `Bands.Verify` (grupo "Bandas", `bandas.md`), que usam o mesmo `UsersGrantedAsync` para avisar quem revisa.


`Admin` tem todas as permissões **sem claim** (`PermissionChecker` devolve o catálogo inteiro). `User` nasce sem nenhuma: usa a API pelos endpoints de dono. Papel de sistema (`IsSystem`) não é editável nem excluível; papel com usuários não é excluível (`ROLE_IN_USE`). O cache de permissões (`HybridCache`) é invalidado em toda escrita de papel/permissão e no seed.

## Token na query: só no hub de notificações

O WebSocket do navegador não manda header, então o hub do SignalR recebe o JWT em `?access_token=`. O `OnMessageReceived` do `AddJwtBearer` só lê esse parâmetro em caminhos `/hubs`; no resto da API, token na query é ignorado. O `RequestDescriptor` mascara o valor (`***`) antes de qualquer log (`observabilidade.md`, `notificacoes.md`).

## Eventos de log (contexto `Auth`)

| Evento | Nível | Quando |
|---|---|---|
| `USER_REGISTERED` | Info | registro commitado (`UserId`) |
| `EMAIL_CONFIRMATION_SEND_FAILED` | Warning | envio do e-mail de confirmação devolveu `false` ou lançou (`UserId`) |
| `PASSWORD_RESET_SEND_FAILED` | Warning | idem para o e-mail de redefinição (`UserId`) |
| `EMAIL_CONFIRMED` | Info | confirmação aceita (`UserId`) |
| `PASSWORD_RESET` | Info | senha redefinida pelo link (`UserId`) |
| `REFRESH_TOKEN_REUSE` | Warning | refresh revogado apresentado fora da janela; todos os tokens do usuário caíram (`ApplicationUserId`) |

Os eventos do login social (`USER_REGISTERED_EXTERNAL`, `EXTERNAL_LOGIN_LINKED`, …) estão em `login-social.md`.

Nenhum deles leva e-mail, token ou senha.

## Roteiro de verificação

Com `docker compose up -d` e a API no ar (`Email:Provider=Smtp` apontando para o Mailpit em http://localhost:8025):

1. `POST /api/auth/register {name:"Caio", nickname:"caio", email:"caio@test.local", password:"abc123"}` → 200 `REGISTRATION_PENDING_CONFIRMATION` com `{userId, email}` e sem `tokens`; log `USER_REGISTERED` e `EMAIL_SENT`; o e-mail aparece no Mailpit com o botão para `http://localhost:5173/confirmar-email?userId=…&token=…`.
   - Mesmo e-mail de novo → 409 `EMAIL_ALREADY_IN_USE`; `nickname:"Caio!"` → 400 `INVALID_NICKNAME`; `nickname:"admin"` → 409 `NICKNAME_TAKEN`; senha `abcdef` → 400 `PASSWORD_TOO_WEAK`.
2. `POST /api/auth/login` antes de confirmar → 403 `EMAIL_NOT_CONFIRMED`; com senha errada → 401 `INVALID_CREDENTIALS`.
3. `POST /api/auth/resend-confirmation {email}` → 200 `CONFIRMATION_EMAIL_SENT` (também para e-mail inexistente, sem e-mail no Mailpit); 4ª chamada na mesma hora → 429 `TOO_MANY_REQUESTS` e `RATE_LIMIT_REJECTED` no log.
4. `POST /api/auth/confirm-email {userId, token}` com o token do link → 200 `EMAIL_CONFIRMED { alreadyConfirmed: false }`; repetir → `alreadyConfirmed: true`; token adulterado → 400 `EMAIL_CONFIRMATION_INVALID`.
5. Login → `LOGIN_SUCCESS` com `data.tokens.token` (sem `refreshToken`) e o `Set-Cookie: nv_refresh=…; path=/api/auth; secure; samesite=strict; httponly` de 7 dias; `GET /api/profile` → `nickname: "caio"`; `POST /api/profile/complete-onboarding` → 404 (endpoint removido).
6. `POST /api/auth/refresh-token` sem corpo, com o cookie e `Origin: http://localhost:5173` → `TOKENS_RENEWED` e cookie novo; sem cookie → 401 `INVALID_TOKEN` sem `Set-Cookie`; com `Origin: https://evil.example` → 403 `ORIGIN_NOT_ALLOWED` e o cookie continua valendo; reapresentar o refresh consumido na hora → 401 `INVALID_TOKEN` e nada mais; reapresentá-lo depois de 30 s → 401, `REFRESH_TOKEN_REUSE` no log e o par novo também passa a dar `INVALID_TOKEN`.
7. `POST /api/auth/forgot-password {email}` → 200 `PASSWORD_RESET_EMAIL_SENT` para existente e inexistente; e-mail no Mailpit com `/redefinir-senha?email=…&token=…`; `POST /api/auth/reset-password {email, token, newPassword:"xyz789"}` → 200 `PASSWORD_RESET_SUCCESS` sem tokens; refresh token anterior → `INVALID_TOKEN`; login com a senha antiga → 401; com a nova → 200; reusar o token de reset → 400 `PASSWORD_RESET_INVALID`.
8. `POST /api/profile/change-password` com atual errada → 401 `INVALID_CREDENTIALS`; nova fraca → 400 `PASSWORD_TOO_WEAK` e a antiga continua valendo; certa → `PASSWORD_CHANGED` com token novo e cookie novo; o cookie anterior → `INVALID_TOKEN`.
9. `POST /api/auth/logout` → `LOGOUT_SUCCESS` e o cookie apagado (`expires=Thu, 01 Jan 1970`); o refresh anterior → `INVALID_TOKEN`.
10. Login `caiogualbertodev@outlook.com` / `12341234` → `GET /api/admin/users?search=cai` → 200 com o usuário; `POST /api/admin/users/invite {name, email, roleId}` → `USER_INVITED` e o e-mail de convite no Mailpit; login do convidado → `GET /api/admin/users` → 403 `PASSWORD_CHANGE_REQUIRED`; `set-initial-password` → tokens; repetir → `PASSWORD_CHANGE_NOT_PENDING`.
11. `DELETE /api/admin/users/{id}` do próprio → `CANNOT_DELETE_SELF`; do último Admin → `LAST_ADMIN_PROTECTED`; de outro → `USER_DELETED`, o login dele vira `INVALID_CREDENTIALS`, e registrar de novo o mesmo e-mail e nickname funciona (tombstone).
12. Admin → `GET /api/admin/users/summary` → `USER_SIGNUP_SUMMARY_FOUND` com `today`, `last7Days`, `last30Days`, `total` e `unconfirmed`; registrar uma conta sem confirmar → `today`, `total` e `unconfirmed` sobem 1. `GET /api/admin/users` → a conta aparece primeiro com `emailConfirmed: false`, `hasPassword: true` e `providers: []`; uma conta de Google traz `providers: [1]`; uma conta revogada some da lista e do `total`. Conta comum → 403 nos dois.
