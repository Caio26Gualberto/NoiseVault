---
paths:
  - "**/NoiseVault.Infra.Data/Auditing/**"
  - "**/NoiseVault.Application/AuditLogs/**"
  - "**/NoiseVault.Domain/Auditing/**"
  - "**/*Audit*"
  - "**/NoiseVault.Infra.Data/Context/AppDbContext.cs"
---

# Decisões — Auditoria

Duas trilhas coexistem de propósito. Antes de criar uma terceira, veja se a pergunta já tem dono.

| Trilha | Pergunta | Como é escrita |
|---|---|---|
| `AuditLog` (global) | "**o quê** mudou nesta linha, de qual valor para qual" | automática, pelo interceptor |
| `AuthorizationAuditLog` | "quem mexeu em papel/permissão/equipe" | manual, pelo AppService |

Um projeto que precise responder "**por quê** a equipe fez isso com este cliente" (motivo, ticket) cria uma terceira trilha manual, no molde da segunda — não afrouxa a primeira.

## Auditoria global: automática, e só da equipe

`AuditLog` é preenchida pelo `AuditSaveChangesInterceptor` (`Infra.Data/Auditing/`), registrado em `AddDbContext(...).AddInterceptors(...)`. **Nenhum AppService chama nada** — é o que garante que ninguém esqueça de auditar.

🔴 **`AuditLog` não herda `EntityBase`, e isso é a trava contra recursão.** O interceptor varre `ChangeTracker.Entries<EntityBase>()`, então a tabela de auditoria é invisível para ele. Uma guarda `if (type == typeof(AuditLog)) continue` seria removível por engano; a hierarquia não é. O preço é o `IAuditLogRepository` próprio, que não estende o genérico.

🔴 **Só grava quando o autor é um usuário autenticado com papel diferente de `User`** (`UserIdOrNull != null && Role != Roles.User`). Esse predicado único exclui o `SeedData`, os jobs, e **todo** o uso pelo usuário comum — a auditoria global existe para responder "o que a equipe mudou", não para logar o produto. Não existe lista de exclusão por volume, e não deve existir — se ação do usuário comum precisar ser auditada, o lugar é outra trilha.

**Por que interceptor e não outro ramo do `ApplyAuditing`**: o `AppDbContext` sobrescreve só `SaveChanges()` e `SaveChangesAsync(CancellationToken)`; a sobrecarga `SaveChangesAsync(bool, CancellationToken)` escapa. `SavingChanges`/`SavedChanges` disparam para todas.

**Duas fases, por causa do `Id`**: a captura acontece em `SavingChanges` (estados e valores) e a gravação em `SavedChanges`. Operação que falha **não** deixa auditoria (`SaveChangesFailed` limpa o pendente). Com transação do `IUnitOfWork` aberta, é atômico dos dois lados; sem transação, perder auditoria é possível, inventá-la nunca.

Ações: `Created`, `Updated`, `SoftDeleted`, `HardDeleted`. **Campo alterado é `IsModified` E valor diferente**: `_dbSet.Update()` marca tudo como modificado, então sem a comparação toda alteração viraria um dump da linha.

**Política de dados sensíveis: `Domain/Auditing/AuditPolicy.cs`.** A lista é o contrato — o que não está nela é auditado.
- Ignoradas: as trilhas de auditoria e `BackgroundJobRun` (ela **é** a trilha do job). Entidade nova que não deve ser auditada entra aqui **no mesmo commit** em que nasce.
- Mascaradas gravam `{ field, masked: true }` sem valores: chave de storage, segredo de integração, token. Saber que mudou é o que a auditoria conta; saber qual é, não.
- Sempre omitidos: os campos de `EntityBase`.

⚠️ **Senha e token de sessão estão fora por herança, não por regra.** `ApplicationUser`, `ApplicationRole` e `RefreshToken` **não herdam `EntityBase`**. Fazer qualquer uma herdar passaria a copiar credencial para a auditoria.

**Retenção não existe.** Quando o volume pedir, o molde é um recorrente do Hangfire apagando por `OccurredAt`. Os índices cobrem a ordenação e os filtros da tela (`(OccurredAt DESC, Id DESC)`, `(EntityType, EntityId)`, `(ActorAccountId, OccurredAt)`, `(Action, OccurredAt)`).

## Roteiro de verificação

1. Login admin → `POST /api/admin/users/invite` → `GET /api/admin/audit?entityType=User` mostra `Created` com os campos do usuário.
2. Mesma ação por um usuário comum (`POST /api/items/{id}/archive`) → nenhuma linha nova em `AuditLog`. ⚠️ Arquivar é `ExecuteUpdateAsync`, que não passa pelo `ChangeTracker`: nem o admin gera `AuditLog` ali. Para ver o interceptor, edite um papel (`PUT /api/admin/authorization/roles/{id}`) — não: `ApplicationRole` não herda `EntityBase`. Convide um usuário: `User` novo → `Created`.
3. `POST /api/admin/authorization/roles` → linha em `AuthorizationAuditLog` (`RoleCreated`); nada em `AuditLog`, porque `ApplicationRole` não herda `EntityBase`.
