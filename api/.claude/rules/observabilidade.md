---
paths:
  - "**/NoiseVault.Api/Middlewares/**"
  - "**/NoiseVault.Api/Diagnostics/**"
  - "**/NoiseVault.Api/RateLimiting/**"
  - "**/NoiseVault.Api/ApiResponse/**"
  - "**/Logging/**"
---

# Decisões — Observabilidade do fluxo do cliente

O objetivo é um só: **toda falha que o cliente vê deixa uma linha no log**, com mensagem, exceção interna, stack, usuário e qual action foi disparada. Um erro só virava log se a exceção subisse até o middleware; há quatro situações, e o middleware sozinho cobre uma.

| | Situação | Quem cobre |
|---|---|---|
| 1 | Exceção sobe (`catch { throw; }` ou sem catch) | `UNHANDLED_EXCEPTION` no middleware |
| 2 | `catch (ex) { _logger.Error(...); return Result.Fail(...); }` | o próprio serviço |
| 3 | `catch (ex) { return Result.Fail(...); }` — exceção destruída no catch | **ninguém**; precisa de `_logger.Error` antes do `return` |
| 4 | Nenhuma exceção: 429, `Result.Fail(..., ErrorType.Unexpected)` | `REQUEST_FAILED` e `RATE_LIMIT_REJECTED` |

A categoria 3 é invisível para qualquer middleware. `catch (Exception)` sem variável é proibido em caminho de cliente, porque destrói o motivo.

## O middleware de saída (categoria 4)

Depois do `await _next(context)`, se a resposta saiu `>= 500` e nenhuma exceção foi lançada, o `ExceptionHandlingMiddleware` emite `REQUEST_FAILED` (Error). `BaseController.MapError` deposita `error.Code` em `HttpContext.Items` (`RequestDescriptor.ErrorCodeItemKey`) e o middleware lê — sem injetar logger no `BaseController`.

🔴 **Só 5xx entra.** 4xx é resposta normal (validação, not found, conflito); logar viraria ruído e treinaria todo mundo a ignorar Error. A única exceção é o 429, tratado pelo rate limiter.

## O que cada linha carrega

`RequestDescriptor.Describe` põe em `data`: `Method`, `Path`, `Action` (`Items.Create`, via `ControllerActionDescriptor`), `Status`, `Query`, `RouteValues`, `Ip`, `AppVersion`/`AppPlatform` (headers `X-App-Version`/`X-App-Platform` que o app manda — é o que identifica requisição **anônima**, como login, onde não há `userId`). O middleware acrescenta `TraceId`, `ElapsedMs`, `AuthenticatedUserId` e `ErrorCode`. O `TraceId` vai no header `X-Trace-Id` da resposta. 🔴 O parâmetro `access_token` da `Query` sai como `***`: é o JWT do hub de notificações (`auth-e-contas.md`).

Body, headers de auth, cookies e upload nunca entram.

## A cadeia de exceções internas

`ApplicationLogEntry.CausedBy` achata `InnerException` (até 5 níveis, `A: msg <- B: msg`). Em `DbUpdateException` o topo é *"See the inner exception"* — a `PostgresException`/`SqlException` com constraint e coluna está no `caused-by`. `ExceptionDetails` é usado pelo `ApplicationLogger` e pelo middleware, então vale para todo `_logger.Error(...)`. O stack tem teto de 8000 caracteres, cortado no fim.

⚠️ O log do middleware inteiro vive num `catch`; se o writer falhar, sai um `LOGGER_FAILURE` de último recurso no `Console.Error`.

## O envelope do 429 é o mesmo do resto

`EnvelopeJson.Options` (`Api/ApiResponse/`) é a única fonte das opções de serialização (camelCase) fora do pipeline MVC. **Nunca chame `JsonSerializer.Serialize(new ApiResponse...)` direto**: serializar sem opções produz PascalCase, o cliente lê `code` minúsculo, cai em `UNKNOWN` e mostra a mensagem genérica — o envelope existe mas está ilegível.

`OnRejected` emite `RATE_LIMIT_REJECTED` (Warning) com política, partição (conta ou IP), rota e action.

## Formato em produção

Produção usa texto por default, e no Logs Insights só há busca por substring (`filter @message like /REQUEST_FAILED/`). Com `Format: "Json"` é possível consultar por campo (`{ $.userId = 371 }`). É uma linha no `appsettings.Production.json` e uma decisão de projeto — registre qual foi.

## Roteiro de verificação

1. Forçar um `throw` num endpoint com inner exception → 500 `UNEXPECTED_ERROR`, header `X-Trace-Id`, linha `UNHANDLED_EXCEPTION` com `Action`, `caused-by=`, `stack=`.
2. `Result.Fail(..., ErrorType.Unexpected)` → linha `REQUEST_FAILED ... Status=500 ErrorCode=<code>`.
3. `Result.Fail(..., ErrorType.Validation)` e 401 → nenhuma linha nova.
4. Estourar o rate limit → corpo camelCase `TOO_MANY_REQUESTS` e `WARN RATE_LIMIT_REJECTED`.
