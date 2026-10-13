---
paths:
  - "**/Headbanger.Application/Auth/**"
  - "**/Headbanger.Application/Users/**"
  - "**/Headbanger.Application/Profiles/**"
  - "**/Headbanger.Application/Authorizations/**"
  - "**/Headbanger.Infra.Data/Identity/**"
  - "**/Headbanger.Domain/Authorization/**"
  - "**/Headbanger.Domain/Entities/User.cs"
---

# Decisões — Usuários e acesso

Um único tipo de usuário entra por dois caminhos: **registro** (público, vira `User`) e **convite** (admin, vira o papel escolhido, com senha temporária). O ciclo de permissões está no `CLAUDE.md`, que é convenção e não decisão.

## Registro: validar antes de escrever, e devolver o motivo

`RegisterUser` devolve `UserCreationResult` com um `EUserCreationFailure` classificado, e `AuthAppService` traduz em `PASSWORD_TOO_WEAK` / `EMAIL_ALREADY_IN_USE` / `INVALID_EMAIL`. **A validação roda antes de qualquer escrita** (`UserValidators` + `PasswordValidators` do próprio `UserManager`, mais `AnyAsync` no e-mail): inserir o `User` e só então chamar `CreateAsync` faria e-mail repetido estourar no índice único e virar 500. Reusar os validators do Identity em vez de reimplementar a política é o que impede a mensagem de mentir quando a configuração mudar.

O registro **já devolve tokens**: não há verificação de e-mail neste starter. Quando o produto exigir (e-mail confirmado antes do primeiro login, código de 6 dígitos, esqueci-a-senha), o lugar é `IAuthenticateService` + `AuthAppService` + um `IEmailService` real — e o `Register` passa a devolver só o id, com o login recusando `EMAIL_NOT_CONFIRMED`.

⚠️ **Regra de senha vive em dois lugares e precisa concordar.** A política está no `AddIdentity` (6 caracteres, um dígito, sem maiúscula/símbolo obrigatórios). O validador dos clientes precisa espelhar — senão o usuário toma erro do servidor num campo que a tela deu por válido.

## Entrada na equipe é por convite, e a senha nunca é digitada por outra pessoa

`POST /api/admin/users/invite` (`Authorization.Users.Invite`, que **não** entra em `RoleDefaultPermissions` — só Admin) cria um usuário com papel e permissões adicionais opcionais. Não existe endpoint em que alguém digita a senha do colega: senha combinada por fora não tem como ser trocada de verdade.

- **O papel vem por id, não por nome**: papéis são dinâmicos (a tela cria os personalizados), então uma lista fixa de nomes envelheceria. Papel `Admin` ignora permissões adicionais — as dele são implícitas.
- A senha sai do `TemporaryPasswordGenerator` e **existe apenas em memória e no e-mail** — nunca no `AuthorizationAuditLog`.
- 🔴 **O e-mail é enviado dentro da transação, antes do commit.** Falha de envio ⇒ rollback + `INVITE_EMAIL_FAILED`: uma conta cuja senha ninguém conhece é pior que um convite não enviado. É a única operação em que uma chamada externa fica dentro da transação, e é deliberado. Com `ConsoleEmailService`, a senha temporária sai no log (`EMAIL_USER_INVITE`).

## Primeiro login: bloqueio no servidor, não só na tela

Quem entra por convite nasce com `IsNeededChangePassword = true`, que vira o claim `password_change_required` no JWT. O `PasswordChangeRequiredFilter` (filtro global) devolve `403 PASSWORD_CHANGE_REQUIRED` em **qualquer** rota enquanto o claim existir, exceto as mínimas para trocar a senha: `profile`, `profile/change-password`, `profile/set-initial-password`, `admin/authorization/me`, `auth/logout`, `auth/refresh-token`.

**São dois endpoints, de propósito.** `change-password` é a troca voluntária e **exige a senha atual**. `set-initial-password` recebe só a nova e **só funciona enquanto a flag for true** (`PASSWORD_CHANGE_NOT_PENDING` caso contrário): pedir a senha atual a quem acabou de copiá-la de um e-mail é atrito sem ganho de segurança. **Os dois devolvem `TokensDto`**, não `bool`: o access token carrega o claim, e sem par novo a pessoa trocaria a senha e continuaria bloqueada até vencer.

## Revogar acesso não apaga histórico

`DELETE /api/admin/users/{id}` **não apaga a linha**: `User` é referenciado com `Restrict` por tudo que registra autor. A operação tem dois lados na mesma transação:

- **Domínio**: soft delete de `Users` com `IsActive = false` — some das listas, continua respondendo pelas FKs.
- **Identity**: purga de verdade — `UserClaims`, `UserRoles`, `RefreshTokens` e o `ApplicationUser` (`RoleManagementService.RevokeUserAccessAsync`). É isso que faz o acesso acabar no ato.

Duas guardas: `CANNOT_DELETE_SELF` e `LAST_ADMIN_PROTECTED` (reusa `IsLastAdminAsync`, que também protege a troca de papel).

🔴 **O e-mail é tombstoneado na exclusão** (`deleted+{id}+{original}`). O índice único de `Users.Email` **não filtra soft-deleted** de propósito (e-mail é identidade, não pode ter duas linhas vivas), então sem o tombstone um novo convite para a mesma pessoa morreria em `EMAIL_ALREADY_IN_USE`. O `Name` permanece — é ele que a auditoria exibe. O e-mail original vai para o `OldValueJson` do `AuthorizationAuditLog`.

## Papéis e permissões: Admin é implícito, o resto é claim

`Admin` tem todas as permissões **sem claim** (`PermissionChecker` devolve o catálogo inteiro) — tentar editar as permissões do Admin devolve `ADMIN_ROLE_IMPLICIT`. `User` nasce sem nenhuma (`RoleDefaultPermissions.User` vazio): ele usa a API pelos endpoints de dono, não pelo painel. Papel de sistema (`IsSystem`) não é editável nem excluível; papel com usuários não é excluível (`ROLE_IN_USE`). Permissão adicional por usuário existe para exceção pontual, não para substituir o papel. O cache de permissões (`HybridCache`, tag `AuthorizationCacheKeys.Tag`) é invalidado em toda escrita de papel/permissão e no seed.

`GET /api/admin/authorization/users` lista **quem tem papel diferente de `User`** — é a lista da equipe do painel. `GET /api/admin/users` lista todos, paginado — é a administração de contas.

## Roteiro de verificação

1. `POST /api/auth/register` com senha `abcdef` → `PASSWORD_TOO_WEAK`; com e-mail já usado → 409 `EMAIL_ALREADY_IN_USE`; válido → `REGISTRATION_SUCCESS` com tokens.
2. Token desse usuário em `GET /api/admin/items` → 403; em `GET /api/items` → 200.
3. Login `admin@headbanger.local` → `POST /api/admin/users/invite` `{name, email, roleId}` → `USER_INVITED`; a senha temporária sai no log (`EMAIL_USER_INVITE`).
4. Login do convidado → `GET /api/admin/items` → 403 `PASSWORD_CHANGE_REQUIRED`; `POST /api/profile/set-initial-password` → tokens novos; com eles, `GET /api/admin/items` → 200; repetir o set-initial → `PASSWORD_CHANGE_NOT_PENDING`.
5. `DELETE /api/admin/users/{id}` do próprio → `CANNOT_DELETE_SELF`; do último Admin → `LAST_ADMIN_PROTECTED`; de outro → `USER_DELETED` e o login dele passa a `INVALID_CREDENTIALS`.
