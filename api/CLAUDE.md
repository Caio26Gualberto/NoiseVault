# CLAUDE.md — Backend (API)

## Sobre este projeto

**Noise Vault** — API da cena do ABC Paulista (metal, punk, hardcore e afins): feed de posts, mural das antigas, eventos, perfis de pessoas e de bandas. Os usuários são fãs e músicos da região; o painel admin é a mesma API com permissão. Reescrita do zero do projeto legado "NewLevel", que não serve de referência de código — só de inventário de funcionalidades.

**Estado da entrega atual:** existem de verdade o módulo **Auth** (registro com confirmação de e-mail, login, refresh, logout, esqueci/redefinir senha, troca de senha), o módulo **Perfil e mídia** (campos do perfil, avatar e capa no S3, perfil público, busca de pessoas — `perfil-e-midia.md`, `armazenamento.md`), o módulo **Bandas e convites** (criar e editar banda, integrantes, convites com aceite atômico — `bandas.md`) e o módulo **Feed** (posts de pessoa e de banda, fotos, vídeo com upload multipart direto ao S3 e conversão por ffmpeg num job, curtidas, comentários — `feed.md`, `midia-e-video.md`) e o módulo **Eventos** (agenda com line-up misto, interesse, comentários, cancelamento e post automático no feed — `eventos.md`). O módulo **Mural das antigas** (arquivo de fotos e fitas com permissão `Archive.*`, linha do tempo, "Eu estava lá" — `mural.md`) substituiu a galeria com moderação. O módulo **Notificações** (curtidas, comentários, eventos, mural e banda, agrupadas; push por SignalR; e-mail só no convite de banda — `notificacoes.md`) também. O módulo **Suporte** (ticket com tags, páginas, fotos e vídeo de até 2 min, caixa de entrada com permissão `Support.*`, conversa com quem reportou — `suporte.md`) fecha a ordem de `.claude/rules/dominio.md`. Fora de escopo: loja, podcasts, app nativo, login social.

## Como trabalhar neste repositório

Arquitetura e convenções do backend. Ao criar ou alterar qualquer feature, siga o padrão canônico descrito aqui. **Enquanto o primeiro módulo de negócio não nasce, a feature de referência para CRUD é a `Items` do boilerplate** em `C:\Users\Administrador\Desktop\Boilerplate\api` (entidade, CRUD paginado, permissões por verbo, update atômico, soft delete, índice único filtrado, rate limit, log, jobs). Ela não está neste repositório de propósito: código de exemplo que compila junto vira código de produção por acidente. Quando o primeiro módulo real (Perfil) existir, ele passa a ser a referência e esta frase muda.

As **decisões** — o porquê de cada regra — vivem em `.claude/rules/`, um arquivo por domínio, com front matter `paths:`. Elas carregam sozinhas quando você toca no código correspondente; **ao planejar antes de abrir arquivo, leia a relevante de propósito.** Este arquivo é convenção; a rule é decisão.

| Arquivo | Cobre |
|---|---|
| `.claude/rules/auth-e-contas.md` | registro com confirmação de e-mail, login, esqueci/redefinir senha, nickname, refresh atômico, convite, revogação, papéis e permissões |
| `.claude/rules/dominio.md` | o modelo da comunidade (pessoa, banda, integrante, convite, conteúdo, moderação), enums herdados e a ordem dos módulos |
| `.claude/rules/perfil-e-midia.md` | campos do perfil, edição, avatar e capa, perfil público, busca de pessoas |
| `.claude/rules/bandas.md` | banda sem login, slug imutável, quem é Owner, convite com estado e aceite atômico, endpoints, limites |
| `.claude/rules/feed.md` | post de pessoa e de banda, visibilidade do post em processamento, quem edita e apaga, curtida, comentário, contadores |
| `.claude/rules/mural.md` | mural das antigas: registro com data aproximada, mídias de arquivo (4 GB/2 h, 40 MB), permissões `Archive.*`, "Eu estava lá", linha do tempo, post no feed |
| `.claude/rules/notificacoes.md` | notificação como dado estruturado, quem recebe cada tipo, agrupamento, SignalR com token na query, e-mail do convite de banda, retenção, jobs |
| `.claude/rules/suporte.md` | ticket de suporte com tags, páginas e contexto do navegador, mídia `Support` (2 min, 200 MB), caixa de entrada com `Support.*`, conversa e ciclo de status, notificações do ticket |
| `.claude/rules/eventos.md` | evento com line-up de banda cadastrada ou nome livre, cidade fora do ABC, próximo/já rolou, cancelar, interesse, comentários, post automático no feed |
| `.claude/rules/midia-e-video.md` | `MediaUpload`, foto de post, vídeo em multipart pré-assinado, conversão ffmpeg no Hangfire, limpeza, CORS/lifecycle do bucket, plano do MediaConvert em produção |
| `.claude/rules/armazenamento.md` | S3 (LocalStack em dev, CloudFront em produção), processamento de imagem, chaves, ordem sobe-grava-apaga |
| `.claude/rules/email.md` | as duas camadas de e-mail, Mailpit em dev, SMTP via MailKit, links, o que nunca vai para o log |
| `.claude/rules/auditoria.md` | as duas trilhas, o interceptor global, política de dados sensíveis |
| `.claude/rules/jobs-hangfire.md` | filas, Wrapper + Trigger, jobs enfileirados com linha de status |
| `.claude/rules/observabilidade.md` | as quatro categorias de falha, `UNHANDLED_EXCEPTION`/`REQUEST_FAILED`/`RATE_LIMIT_REJECTED`, cadeia de exceções |
| `.claude/rules/abuso-e-limites.md` | rate limit por IP/conta e limites de volume por regra de negócio |

**Rule nova entra nesta tabela no mesmo commit.** Rule fora do índice é rule que ninguém lê ao planejar.

O que é do negócio nasce em rules novas. `User`, auth, permissões, jobs e logging não são negócio: são o esqueleto, herdado do boilerplate. `Controllers/Admin/`, `Users/` e `Authorizations/` ficam porque o painel de administração (papéis, verificação de banda) o mural das antigas (`Controllers/Admin/ArchiveAdminController.cs`) e a caixa de entrada de suporte (`Controllers/Admin/SupportAdminController.cs`) usam essa superfície.

## Stack e projetos

.NET 10 · ASP.NET Core · EF Core (Npgsql; o `DatabaseProvider` ainda sabe falar SQL Server, mas o projeto escolheu Postgres no dia 1) · Identity + JWT · Hangfire (jobs) · MailKit (e-mail SMTP) · AWSSDK.S3 (arquivos; LocalStack em dev) · SixLabors.ImageSharp (imagens).

| Projeto | Caminho | Responsabilidade |
|---|---|---|
| `NoiseVault.Api` | `src/NoiseVault.Api` | Controllers, envelope de resposta, `CurrentUser`, middleware, rate limit, `Program.cs` |
| `NoiseVault.Application` | `src/NoiseVault.Application` | AppServices (casos de uso), DTOs, `Result<T>`, abstrações (`IRepository`, `ICurrentUser`, logging, jobs, e-mail) |
| `NoiseVault.Domain` | `src/NoiseVault.Domain` | Entidades (`EntityBase`), enums, constantes, catálogo de permissões, política de auditoria |
| `NoiseVault.Infra.Data` | `src/NoiseVault.Infra.Data` | `AppDbContext`, `Repository<T>`, `UnitOfWork`, Identity, logging, migrations, serviços externos |
| `NoiseVault.Infra.IoC` | `src/NoiseVault.Infra.IoC` | Registro de DI (`DependencyInjection.cs`, `DependencyInjectionJobs.cs`) |
| `NoiseVault.Workers` | `src/NoiseVault.Workers` | Jobs Hangfire (roda dentro do processo da API) |

Dependência entre camadas: `Api → Application → Domain`; `Infra.Data` implementa as abstrações de `Application`; `Workers` referencia `Application`; `Infra.IoC` amarra tudo.

| Camada | Pode depender de | Não pode | Não entra aqui |
|---|---|---|---|
| `Domain` | nada | qualquer outra camada | EF, HTTP, lógica de caso de uso |
| `Application` | `Domain` | EF, Hangfire, `Microsoft.Extensions.Logging`, `Infra.*` | `DbContext`, `HttpContext`, `BackgroundJob.Enqueue` |
| `Infra.Data` | `Application`, `Domain` | `Api`, `Workers` | regra de negócio |
| `Workers` | `Application`, `Domain` | `Infra.Data`, `Api` | lógica de negócio (Trigger só delega) |
| `Api` | `Application`, `IoC`, `Workers` | `Infra.Data` direto | lógica de negócio, mapeamento |

**Não existe projeto de teste na solução.** Verificação é por execução real: `dotnet build`, subir a API e seguir o `## Roteiro de verificação` da rule do domínio. Se a stack de um projeto novo pedir testes, eles entram em `tests/` sem mudar o resto.

## Rotas e escopo

Uma única superfície HTTP: todo controller herda `ApiBaseController` (`[ApiController] [Authorize]`) e vive em `src/NoiseVault.Api/Controllers/`. A organização é por **escopo de acesso**, não por tipo de usuário:

- `Controllers/*.cs` — `/api/...`: o que qualquer usuário autenticado faz sobre o que é dele (`auth`, `profile`). Sem `[Authorize(Policy)]`; o filtro de dono é `UserId == _currentUser.UserId` no `WHERE`.
- `Controllers/Admin/*.cs` — `/api/admin/...`: o que exige permissão do catálogo (`users`, `authorization`, `audit`, `jobs`, `archive`, `support`). **Toda action tem `[Authorize(Policy = Permissions.X.Y)]`.**

O mesmo token serve para os dois; o que separa é a permissão. Superfície nova de verdade (webhook de provedor, integração servidor-a-servidor) ganha pasta própria, base controller próprio e validação própria — não reutilize `ApiBaseController` para request que não tem usuário.

Rotas são explícitas, minúsculas/kebab-case (`/api/items/{id}/archive`), sem `[controller]` token e sem `/v1`.

## Modelo de usuário

Um tipo de usuário: `User` (`Name`, `Nickname`, `Email`, `IsActive`) herda `EntityBase` e é **sempre uma pessoa** — banda é entidade própria sem login (`.claude/rules/dominio.md`). `ApplicationUser` (Identity) aponta 1:1 para `User` via `UserId` e carrega `EmailConfirmed` e `IsNeededChangePassword`. Papéis/permissões vivem **só** no Identity (Roles/UserRoles/RoleClaims/UserClaims) — não existe Role denormalizada. Papéis de sistema: `User` (default do registro, sem permissão) e `Admin` (todas implícitas). O painel cria papéis intermediários pela tela; o código não precisa conhecê-los.

**Auth é stateless (JWT), uma audience.** O access token vale 15 min e é validado só por assinatura + `exp` + audience (`JwtAudiences.Api`). Claims: `user_id`, roles do Identity, `password_change_required` quando houver. O refresh token (7 dias, tabela `RefreshTokens`) é guardado **só como hash SHA-256**; a rotação revoga o token consumido atomicamente e reuso fora da janela derruba todos. "Sair" = revogar os refresh tokens do usuário.

Fluxo: `POST /api/auth/register` (nome, nickname, e-mail, senha → papel `User`, **pendente de confirmação**, sem tokens), `confirm-email`, `resend-confirmation`, `login` (recusa `EMAIL_NOT_CONFIRMED`), `refresh-token`, `logout`, `forgot-password`, `reset-password`; `POST /api/profile/change-password` (exige a atual) e `set-initial-password` (só com `password_change_required`, para quem entrou por convite). A tela de boas-vindas do primeiro acesso é só do front (`localStorage`). Decisões em `.claude/rules/auth-e-contas.md`; e-mail em `.claude/rules/email.md`. Login social ficou de fora: quando entrar, entra no `AuthAppService` e no `IAuthenticateService`, não num serviço paralelo.

## Padrão canônico de feature

Fluxo ponta-a-ponta de uma listagem (exemplo com a `Items` do boilerplate; aqui o mesmo desenho vale para qualquer módulo):

```
ItemsController.List → IItemAppService.GetMine → QueryOptions<Item> → IItemRepository
→ Repository<T> (EF, filtro soft-delete) → AppDbContext → ItemDto (mapeamento manual)
→ Result<PagedResultDto<ItemDto>> → BaseController.ToResponse → ApiResponse<T>
```

Os arquivos citados como "referência" nas seções abaixo (`ItemsController`, `ItemAppService`, `IItemRepository`) vivem no boilerplate externo. Neste repositório, a referência viva de AppService é `Application/Auth/AuthAppService.cs` e de controller é `Controllers/AuthController.cs`.

### Layout por feature em `Application`

```
src/NoiseVault.Application/{Feature}/
  {Feature}AppService.cs
  Interfaces/I{Feature}AppService.cs
  Interfaces/I{Entidade}Repository.cs      ← só quando precisa de SQL atômico/agregado
  Dtos/{...}Dto.cs
```

### Controller

Arquivo de referência: `src/NoiseVault.Api/Controllers/AuthController.cs` (e `Controllers/ItemsController.cs` do boilerplate para CRUD).

- Herda `ApiBaseController` e retorna **sempre** via `ToResponse(result)` — nunca montar `ApiResponse` manualmente nem escolher status code na mão. Ações públicas (login, register) usam `[AllowAnonymous]` por action.
- Controller não contém lógica de negócio nem mapeamento — só delega ao AppService.
- Não existe MediatR/CQRS neste projeto. Não introduzir.

### Envelope de resposta e erros

- Todo AppService retorna `Result<T>` ou `Result` (`Application/Response/Result.cs`): `Result<T>.Ok(data, "CODE", "message")` / `Result<T>.Fail(new Error("CODE", "message", ErrorType.X))`.
- **`Result<T>` é só para métodos que um endpoint chama.** Método público de AppService que existe para outro AppService ou para um job devolve o tipo cru (`Task<int>`, `Task<bool>`) — `IItemAppService.ArchiveExpired` e `Snapshot` são o exemplo. `Result<T>` carrega code/message HTTP que nunca vira resposta, e obriga o chamador a desembrulhar `IsSuccess`/`Data` sem ter o que fazer com a falha.
- Códigos em SCREAMING_SNAKE_CASE e mensagens em inglês (`ITEM_NOT_FOUND`, `"Item not found"`).
- **O `Code` é contrato de i18n, não só log.** Os clientes traduzem a resposta **pelo code** — a `Message` é fallback para code desconhecido. Consequências ao criar um `Result` novo: o code precisa ser **estável** (renomear quebra a tradução) e **específico** (dois erros diferentes não compartilham code, senão o usuário lê a mensagem errada); vale para **sucesso** também; todo code novo é espelhado nos clientes que o exibem.
- `BaseController.ToResponse/MapError` traduz `ErrorType` em status HTTP: `Validation`→400, `NotFound`→404, `Unauthorized`→401, `Forbidden`→403, `Conflict`→409, demais→500. A resposta é sempre `ApiResponse<T>` (`{ isSuccess, code, message, data }`, camelCase).
- Validação é **imperativa dentro do AppService**, retornando `Result.Fail(... ErrorType.Validation)`. Não usar FluentValidation nem DataAnnotations. Mensagem específica > genérica: mensagem genérica não deixa o usuário parado, faz ele inventar a regra.
- Sinal de desfecho numa resposta `Ok` vai como **bool tipado no DTO**, não como code novo.

### AppService

Arquivo de referência: `src/NoiseVault.Application/Auth/AuthAppService.cs` (e `Application/Items/ItemAppService.cs` do boilerplate para CRUD paginado).

- Mapeamento entidade↔DTO é **manual** (sem AutoMapper). Nunca retornar entidade de domínio para fora do AppService — sempre DTO.
- 🔴 **Precisou de lógica de outra entidade? Injete o AppService dela — não crie um AppService novo para o cruzamento.** A lógica mora no AppService **dono da entidade**. AppService novo continua certo quando a feature não tem entidade dona (um dashboard) ou quando o consumo é de outra superfície.
- Usuário logado: injetar `ICurrentUser` (`Application/Abstractions/Authentication/ICurrentUser.cs`, implementado em `Api/Authentication/CurrentUser.cs`). É a **única** abstração de usuário atual — não criar outra.
  - ⚠️ `Email`, `Name` e `UserId` **lançam `UnauthorizedAccessException`** fora de request HTTP. Só `UserIdOrNull`, `Role` e `IpAddress` são null-safe — código que roda em job só pode usar esses.
- **Guarda de concorrência vive no `WHERE`, não num `if` depois do `SELECT`.** Transição de estado que pode sofrer corrida (claim, ativar, arquivar, reservar) é um `ExecuteUpdateAsync` condicional no repositório específico, e "zero linhas afetadas" é a recusa. `IItemRepository.TryActivateAsync`/`TryArchiveAsync` são a referência.
- Chamada externa (e-mail, provedor, fila) acontece **depois** do commit e nunca derruba o caminho principal: falha vira log + `Result` específico, ou log + seguir (ver `Create` de `Items`, que só avisa quando o enfileiramento falha).

### Camada de dados

- Repositório genérico `IRepository<T>` + `QueryOptions<T>` (`Application/Abstractions/Repository/`): `Predicate`, `Includes`, `OrderBy`, `AsNoTracking`, `IncludeDeleted`, `Skip/Take`. Implementação em `Infra.Data/Repositories/Repository.cs` — aplica filtro de soft-delete automaticamente. **Padrão default:** use o genérico.
- **Repositório específico por entidade (casos excepcionais):** quando uma operação precisar de SQL atômico (`ExecuteUpdateAsync` condicional) ou de **projeção agregada** (`GroupBy` numa ida ao banco). Crie um repositório **que estende o genérico**:
  - Interface `I{Entidade}Repository : IRepository<{Entidade}>` co-locada na feature: `Application/{Feature}/Interfaces/`.
  - Implementação `{Entidade}Repository : Repository<{Entidade}>` em `Infra.Data/Repositories/{FeaturePlural}/`. Herda `_context`/`_dbSet` (protected).
  - Registrar no `AddInfra` (o genérico continua registrado).
  - **Subpasta no plural** (`Items`, não `Item`): namespace terminando no nome do modelo colide com o tipo (`CS0118`).
  - **Regra de contenção:** só entra no específico o que realmente precisa de SQL custom; o resto continua no genérico.
  - Entidade que **não** herda `EntityBase` (`AuditLog`) não é alcançada pelo genérico — o repositório dela é independente.
- Escritas com múltiplas operações usam transação via `IUnitOfWork` — exemplo real: `AuthAppService.Register`. ⚠️ `Repository<T>` chama `SaveChangesAsync` a **cada** operação, então um caso de uso com 4 escritas dispara 4 `SaveChanges` dentro da mesma transação.
- Entidades herdam `EntityBase`. Auditoria (`CreatedAt/By`, `UpdatedAt/By`, `DeletedAt/By`) é preenchida **automaticamente** pelo `ApplyAuditing` do `AppDbContext` — nunca setar data ou autor na mão.
- 🔴 **`SoftDelete` e `DeleteAsync` são coisas diferentes.** `SoftDelete` marca `IsDeleted = true` e a linha continua respondendo por FKs e auditoria; `DeleteAsync` apaga de verdade. FKs são `Restrict`, então `DeleteAsync` numa entidade referenciada lança. O padrão é `SoftDelete`.
  - O carimbo de exclusão é disparado pela **transição** (`IsDeleted = true` com `DeletedAt` nulo), é idempotente e vale para quem marca `IsDeleted` direto.
  - Reviver uma linha exige **zerar `DeletedAt`** junto com `IsDeleted = false`.
- Índice único sobre entidade com soft delete precisa de **filtro**: `.HasFilter(DatabaseProvider.SoftDeleteIndexFilter(this))`. Sem ele, apagar e recadastrar a mesma chave colide contra uma linha invisível. `Items(CustomerId, Name)` é o exemplo.
- Violação de unicidade **não é o caminho normal de erro**: o AppService checa antes e devolve `Conflict` (`ITEM_NAME_TAKEN`); o índice é a rede de segurança contra a corrida, e quando ela pega, o 500 sai logado com `caused-by`.
- Configuração EF é fluent **inline** no `OnModelCreating` do `AppDbContext` — não criar classes `IEntityTypeConfiguration` separadas.
- Migrations em `Infra.Data/Migrations/`:
  ```
  dotnet ef migrations add NomeDaMigration --project src/NoiseVault.Infra.Data --startup-project src/NoiseVault.Api
  ```
  ⚠️ **Rebuildar antes do `database update`** — com `--no-build` o snapshot recém-gerado não está compilado e o EF acusa "pending model changes". `Database:MigrateOnStartup = true` só em desenvolvimento.

### Postgres vs SQL Server

`Database:Provider` (`Postgres` default | `SqlServer`) decide o provider EF e o storage do Hangfire em **um** lugar cada: `Infra.Data/Context/DatabaseProvider.cs` e `Program.cs`. O restante do código não sabe qual banco está embaixo, e é para continuar assim.

| Diferença | Onde está tratada |
|---|---|
| Filtro de índice único (`"IsDeleted" = false` vs `[IsDeleted] = 0`) | `DatabaseProvider.SoftDeleteIndexFilter` / `NotNullIndexFilter` |
| Coluna JSON (`jsonb` vs `nvarchar(max)`) | `DatabaseProvider.JsonColumnType` |
| Colação case-insensitive de e-mail | só Postgres (`Database.IsNpgsql()` no `OnModelCreating`); SQL Server já é CI por default |
| `timestamp with time zone` vs `datetime2` | EF mapeia sozinho; o código sempre grava `DateTime.UtcNow` |
| `ExecuteUpdateAsync`, `text[]`, `RETURNING` | evite `PrimitiveCollection` e SQL cru; se precisar, isole no repositório específico e trate os dois providers |

🔴 **Migration é gerada por provider.** Trocar o provider depois da primeira migration exige regerar a pasta `Migrations/` do zero. Um projeto escolhe o banco no dia 1.

### Paginação

Toda listagem nasce paginada. O par canônico vive em `Application/Pagination/`:

- **Entrada**: `PagedRequestDto` (`Page`, `PageSize`, `Skip`, `Normalize()` que clampa `Page >= 1` e `PageSize` entre 1 e `MaxPageSize`, hoje **100**). O filtro de cada feature **herda** dele (`ItemListQueryDto`) e é bindado com `[FromQuery]`. Chamar `Normalize()` como primeira linha.
- **Saída**: `PagedResultDto<T>` (`Items`/`Page`/`PageSize`/`TotalCount` + calculados), montado por `PagedResultDto<T>.Create(...)`.

O AppService monta **um** `QueryOptions` e o usa nas duas chamadas — `CountAsync(filter)` e `ListAsync(filter)`. `CountAsync` ignora `Skip`/`Take` de propósito. Referência: `ItemAppService.ListAsync`.

Nunca paginar em memória (`ListAsync().Skip().Take()`); contagem é `CountAsync`, nunca `ListAsync().Count`. **Ordenação precisa de desempate**: `OrderByDescending(CreatedAt)` sozinho não é determinístico — toda listagem acrescenta `.ThenByDescending(x => x.Id)`.

### Permissões: toda funcionalidade nova prevê a sua

🔴 **Endpoint em `Controllers/Admin/` sem `[Authorize(Policy = ...)]` é bug de revisão.** A permissão faz parte da entrega. O ciclo completo, todo obrigatório:

1. Constante em `Domain/Constants/Permissions.cs` (SCREAMING dot-case: `Items.View`).
2. Declaração em `Domain/Authorization/PermissionCatalog.cs`, com grupo e o pai implícito quando houver. **`PermissionCatalog.Exists` é o portão**: nome fora do catálogo não vira policy, e o `PermissionPolicyProvider` deixa cair no provider padrão — o endpoint fica aberto sem ninguém perceber.
3. Concessão padrão por papel em `Domain/Authorization/RoleDefaultPermissions.cs` — senão o papel que deveria usar a tela toma 403. `Admin` tem tudo implícito.
4. `[Authorize(Policy = Permissions.X.Y)]` na action.
5. **Espelhar no front**: a mesma string em `src/auth/permissions.ts` do painel, e a tela/ação consumindo-a.

Não existe registro manual de policy: `Api/Authorization/PermissionPolicyProvider.cs` cria uma sob demanda para qualquer nome do catálogo. `SeedData.SeedRolePermissionsAsync` **reconcilia** a cada boot: concede o que falta em relação ao `RoleDefaultPermissions` e remove claim cujo nome saiu do catálogo — sem mexer em permissão concedida à mão.

#### Granularidade: um verbo por ação, não um `Manage` guarda-chuva

🔴 **`Manage` só se sobrar exatamente uma ação de escrita.** Uma feature com CRUD nasce com `View` / `Create` / `Edit` / `Delete` — os três últimos declarando `View` como pai. Sem isso não existe o caso mais comum de todos: *deixar alguém ver a lista sem poder mexer em nada*. `Items.*` e `Authorization.Roles.*` são a referência.

- **No front, cada botão verifica a sua própria capacidade.** Uma coluna de ações inteira atrás de uma permissão só devolve o problema pela porta dos fundos.
- **Leitura auxiliar fica no `View`**, não no verbo de escrita.
- **Não criar verbo antes da ação existir.** Permissão declarada e nunca verificada é documentação falsa.

Endpoints fora de `Admin/` não usam permissão: o usuário é dono do que é dele, e o filtro é `UserId == _currentUser.UserId` no `WHERE` (nunca um `if` depois de carregar a linha de outro).

### Checklist: nova feature/endpoint

1. Entidade em `Domain/Entities/` herdando `EntityBase`; mapear no `OnModelCreating` (índice único com filtro, FKs `Restrict`); criar migration.
2. Pasta `Application/{Feature}/` com `Dtos/`, `Interfaces/I{Feature}AppService.cs` e `{Feature}AppService.cs` retornando `Result<T>`; `SetOperation` no início dos métodos que importam.
3. Registrar no `AddInfra` (`Infra.IoC/DependencyInjection.cs`), na seção certa.
4. Controller em `Controllers/` (escopo do próprio usuário) ou `Controllers/Admin/` (exige permissão), herdando `ApiBaseController`, rota kebab-case, e `ToResponse`.
5. Permissão do endpoint: os 5 passos acima, com granularidade fina.
6. Se a action lista algo, ela é paginada e ordenada com desempate.
7. Se a action pode ser abusada (cria, envia, chama fora), ela tem política de rate limit (`Api/RateLimiting/`).
8. Se precisa de trabalho fora do request, ver `.claude/rules/jobs-hangfire.md`.

### O que atualizar depois de codar

- Code novo de erro/sucesso → espelhar nos clientes que o exibem.
- Permissão nova → os 5 passos, inclusive o espelho no `frontend`.
- Evento de log novo → tabela de eventos da rule do domínio.
- Job novo → tabela de política de retry em `jobs-hangfire.md`, fila em `options.Queues` no mesmo commit.
- Decisão nova, alternativa descartada, incidente → rule do domínio (título `# Decisões — X`, seções-tese). Rule nova → tabela no topo deste arquivo.
- Armadilha resolvida → fica ~~tachada~~ em "Armadilhas conhecidas", não some.

## Fuso horário: UTC no banco, local na borda

O banco guarda **instantes em UTC** — o `ApplyAuditing` grava `DateTime.UtcNow`. Isso não muda por projeto.

🔴 **`Application/Common/Time/AppTime.cs` é o único lugar do servidor que sabe o que é "hoje".** A zona vem de `App:TimeZone` (`AppTime.Configure` no `AddInfra`). Toda janela de dia/mês (`TodayStartUtc`, `MonthStartUtc`, `DayStartUtc`/`NextDayStartUtc` para filtros `from`/`to`) nasce dele, construída na zona da aplicação e materializada em UTC para o `WHERE`. `DateTime.UtcNow.Date` em AppService é bug: fecha o dia na hora errada. `DateTime.Now` nunca aparece no código.

## Logging: infraestrutura própria, contextual, uma linha por evento

🔴 **Não injete `ILogger<T>` em código novo.** A API tem logger próprio, com contratos em `Application/Abstractions/Logging/` e implementação em `Infra.Data/Logging/` — a `Application` não conhece `Microsoft.Extensions.Logging` nem ASP.NET Core.

```csharp
public ItemAppService(IApplicationLoggerFactory loggerFactory)
{
    _logger = loggerFactory.Create(LogContext.Items);
}
```

`LogContext` é **domínio de negócio** (enum em `Application/Abstractions/Logging/LogContext.cs`; `Api` é só fallback do middleware). Feature nova de domínio novo acrescenta um valor.

### O store é o que faz o log valer alguma coisa

`_logger.SetOperation("CreateItem")` e `_logger.Set("ItemId", id)` não emitem log: alimentam o snapshot que sai no campo `data` de todo evento daquele scope — inclusive do `UNHANDLED_EXCEPTION` que o middleware escreve quando a exceção sobe. O transporte é o `ILogContextStore` Scoped (não `BeginScope`): o contexto precisa sobreviver ao unwind da exceção até o middleware.

- **`Activate` é último-vence.** Criar o logger pela factory não ativa nada; o contexto é ativado em `SetOperation`, `Set` ou num dos métodos de log.
- Chave nula ou vazia é ignorada em silêncio. Logging nunca derruba o caminho feliz.

### O writer não pode falhar

Dois writers, ambos Singleton, ambos escrevendo **exatamente uma linha no stdout** por evento. Sem Serilog, sem NLog, sem arquivo local. `Logging:ApplicationLog:Format`: `Json` dá o `ConsoleApplicationLogWriter`, qualquer outro valor (e a ausência) dá o `TextApplicationLogWriter`.

Formato texto (leitura humana): `LEVEL dd/MM/yyyy HH:mm:ss (context) EVENT_NAME: message | user= op= | data | extra | error= | caused-by= | stack=`. Carimbo na zona da aplicação. `error=`, `caused-by=` (cadeia de `InnerException`, até 5 níveis) e `stack=` (teto de 8000 chars, cortado no fim) são seções próprias. **Nenhum campo pode introduzir `\n`.** O JSON é para quando o Logs Insights precisar filtrar por campo em vez de substring — o trade-off explícito de ligar `Format: "Json"`.

### Evento sem falha

```csharp
_logger.Info(
    eventName: "ITEM_ARCHIVED",
    message: "Item archived",
    userId: _currentUser.UserIdOrNull,
    extra: new { ItemId = id });
```

`eventName` é SCREAMING_SNAKE_CASE e agrupa no CloudWatch — mesma disciplina do `Code` do `Result`, mas **não** é a mesma string e não vai para o i18n. O critério do que logar é **evento que muda estado importante ou anomalia que alguém vai investigar** — não passo de execução. Silêncio deliberado (reentrega idempotente, evento ignorado por design) é escolha registrada na rule, não esquecimento.

### Segurança: nada é capturado automaticamente

🔴 **Quem escolhe o que entra no log é o chamador.** Nunca registre senha, código de verificação, JWT, refresh token, `Authorization`, chave de API, connection string, dado de cartão ou payload íntegro de provedor — `Set` e `Extra` só recebem o que for seguro. Body, headers de auth, cookies e upload nunca entram. Query string e route values entram (são identificador, não segredo).

### O middleware cobre exceção que sobe e 500 sem exceção

`ExceptionHandlingMiddleware` recebe os Scoped **por parâmetro do `InvokeAsync`** (o middleware é Singleton). Emite `UNHANDLED_EXCEPTION` (exceção chegou) e `REQUEST_FAILED` (resposta `>= 500` sem exceção — `Result.Fail(..., ErrorType.Unexpected)`), ambos com `Action`, `RouteValues`, `Query`, `Ip`, `X-App-Version`/`X-App-Platform`, `TraceId`, `ElapsedMs`. O `TraceId` volta no header `X-Trace-Id`. **Só 5xx é logado** — 4xx é resposta normal. O rate limiter emite `RATE_LIMIT_REJECTED` (Warning). Detalhes em `.claude/rules/observabilidade.md`.

🔴 **O middleware só enxerga exceção que chega até ele.** `catch { throw; }` chega; `catch { return Result.Fail(...); }` **não** chega. Todo `catch` que consome exceção e devolve `Result.Fail` precisa de `_logger.Error(...)` antes do `return` — e `catch (Exception)` sem variável destrói o motivo antes de existir chance de logar.

## Integrações externas

- `HttpClient` sempre por `AddHttpClient` (typed client), nunca `new HttpClient` por chamada.
- Serviço externo tem abstração em `Application/Abstractions/` ou `Application/Interfaces/` e implementação em `Infra.Data/`. Quando a configuração está ausente, a implementação degrada de forma explícita (`ConsoleEmailService` é o exemplo: loga em vez de enviar) e o boot avisa — nunca lança no caminho feliz por integração opcional faltando.
- Segredo vem de configuração/variável de ambiente; nunca em Markdown nem em Git. `appsettings.Production.json` só carrega o bloco `Logging`.

## Convenções gerais

- Namespace = caminho físico: `Noise Vault.{Camada}.{Feature}[.Subpasta]`. Pastas de feature no plural.
- Sufixos: `I{X}AppService`/`{X}AppService`, `{X}Dto` (geral), `{X}InputDto`/`{X}RequestDto` (entrada), `{X}QueryDto` (filtro paginado), `{X}ResponseDto` (saída); entidades sem sufixo; enums com prefixo `E`.
- **Não escrever comentários no código, e isso inclui `/// <summary>`.** O que precisa de explicação vem para este documento ou para a rule do domínio — o nome do símbolo carrega a intenção. Linha comentada nova é bug de revisão.
- Idioma: mensagens de exceção internas em PT-BR; códigos e mensagens de API em inglês; documentação em PT-BR.
- Não introduzir MediatR, AutoMapper, FluentValidation ou libs equivalentes — o padrão do projeto é explícito/manual.
- Não parametrizar sem segundo caso real: configuração especulativa é removida, não mantida "para o futuro".

## Como o agente trabalha aqui

### Antes de escrever código

1. Ler "Sobre este projeto", este arquivo inteiro e a rule do domínio que a task toca (tabela no topo). Se a task cria domínio novo, ler `dominio.md` de qualquer jeito — é o mapa das entidades e da ordem dos módulos.
2. Localizar a feature de referência para o que vai fazer: CRUD paginado → `Items` do boilerplate; auth/usuários → `Auth/`, `Users/` e `auth-e-contas.md`; job → `jobs-hangfire.md`; logging → `observabilidade.md`.
3. Confirmar se o endpoint é do próprio usuário (`Controllers/`) ou de administração (`Controllers/Admin/`, com permissão). Validação, regra e o que a API devolve são daqui, mesmo quando a task fala em tela.
4. Se a decisão não é óbvia (alternativas reais, impacto em cliente, dinheiro, segurança), registrar a escolha na rule **antes** de codar — a rule é a spec.

### Durante

- Copiar a estrutura da referência, não inventar uma paralela. Abstração nova só com segundo caso real.
- Respeitar as fronteiras da tabela de camadas. Se precisou de `using` de `Infra.Data` na `Application`, o desenho está errado.
- Guarda de concorrência no `WHERE`; validação antes de qualquer escrita; chamada externa depois do commit.
- Todo `catch` que engole loga. Todo endpoint de admin tem permissão. Toda lista é paginada.
- Migrar legado só quando tocado — não reescrever o que a task não pediu.
- Não "consertar" silêncio deliberado nem reintroduzir o que uma rule diz que foi removido de propósito.

### Depois

- Rodar `dotnet build` e o roteiro de verificação da rule (ou escrever um, se a feature é nova).
- Atualizar o que a seção "O que atualizar depois de codar" lista. Doc e código vão no **mesmo commit**.
- Divergência entre doc e código encontrada no caminho se **registra** em "Armadilhas conhecidas", não se apaga.

### Hierarquia de regras (quando duas coisas se contradizem)

1. Rule do domínio (`.claude/rules/*.md`) — é decisão registrada.
2. Este `CLAUDE.md` — convenção do repositório.
3. `CLAUDE.md` da raiz do monorepo, se existir.
4. Convenção do framework/lib.

Uma rule que contradiz este arquivo está dizendo que aquele domínio é exceção — e diz o porquê. Se não diz, está errada, e a correção vai na rule.

## Verificação

Sem projeto de teste: verificação é por execução real.

1. `dotnet build` limpo.
2. Subir a API (`ASPNETCORE_ENVIRONMENT=Development`; `Database:MigrateOnStartup` aplica a migration e o seed cria `caiogualbertodev@outlook.com` com `Seed:AdminPassword`).
3. Seguir o `## Roteiro de verificação` da rule do domínio, com resultado esperado por passo (codes, status HTTP, linha de log).
4. Para 500: conferir a linha `UNHANDLED_EXCEPTION`/`REQUEST_FAILED` com `Action`, `user=`, `caused-by=` e o `X-Trace-Id` na resposta.

## Armadilhas conhecidas

- Existe apenas uma abstração de usuário atual: `ICurrentUser`. Usada pelos AppServices e pela auditoria do `AppDbContext` (`CreatedBy`/`UpdatedBy`/`DeletedBy` guardam `UserId`, sem FK).
- `ExecuteUpdateAsync` não passa pelo `ChangeTracker`: não dispara `ApplyAuditing` nem o interceptor de `AuditLog`. Transição atômica que precisa de trilha grava a trilha por conta própria.
- `Repository<T>.GetByIdAsync` usa `FindAsync` e **não aplica** o filtro de soft delete — use `FirstOrDefaultAsync(QueryOptions)` quando isso importar.
- Middleware é Singleton: dependência Scoped entra por parâmetro do `InvokeAsync`, nunca pelo construtor.
- Logging é `IApplicationLoggerFactory`, não `ILogger<T>` — e o contexto viaja pelo `ILogContextStore` Scoped, não por `BeginScope`.
- O login devolve o token em `data.tokens.token`. O `frontend` é a comunidade, não um painel: qualquer `User` entra; `Admin.Access` só abre as telas de administração.
- `Hangfire` enfileira em `default` qualquer método sem `[Queue]`, e a fila `default` está na lista do servidor só para isso não ficar invisível — mesmo assim, job novo declara a fila.
- ~~Nenhum job de negócio existia~~: o módulo 3 criou os primeiros (`media:cleanup` recorrente e a conversão de vídeo enfileirada, `jobs-hangfire.md`). `EBackgroundJobType` continua vazio: a conversão usa um método próprio no `IEnqueuedJobDispatcher` (`EnqueueVideoTranscode`), não a linha de status `BackgroundJobRun`.
- A conversão de vídeo chama o `ffmpeg` do PATH. Visual Studio ou terminal abertos antes de instalar não enxergam o PATH novo: reinicie, ou passe `Video__FfmpegPath` e `Video__FfprobePath`.
- A colação `case_insensitive` de `Users.Email` é não determinística: Postgres recusa `LIKE`/`Contains` nela. Busca textual é por `Name`/`Nickname` (colação default); e-mail só por igualdade.
- O pacote transitivo `Newtonsoft.Json 11.0.1` (via Hangfire) dispara `NU1903` no build. Aviso, não erro; herdado do boilerplate e pendente de bump.
- Os tokens de confirmação de e-mail e de redefinição de senha dependem das chaves do Data Protection. Em container sem volume, reiniciar a API invalida todo link pendente — `PersistKeysToDbContext` antes do primeiro deploy (`auth-e-contas.md`).
- `Infra.Data` usa `FrameworkReference` do ASP.NET Core (o pacote `Microsoft.AspNetCore.Identity 2.3` do boilerplate foi removido): sem isso o `DataProtectorTokenProvider` resolvia para a versão 2.x e o `PasswordResetTokenProvider` não compilava.

## Como este arquivo evolui

Este arquivo descreve o padrão; a rule descreve a decisão. Convenção que vale para qualquer projeto volta para o boilerplate em `C:\Users\Administrador\Desktop\Boilerplate`; regra do negócio do Noise Vault fica na rule deste repositório. O teste é: *"se eu apagar este projeto, essa frase continua verdadeira?"* — se sim, é convenção.
