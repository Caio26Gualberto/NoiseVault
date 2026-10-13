---
paths:
  - "src/auth/**"
  - "src/api/**"
  - "src/pages/entrar/**"
  - "src/pages/criar-conta/**"
  - "src/pages/confirmar-email/**"
  - "src/pages/esqueci-senha/**"
  - "src/pages/redefinir-senha/**"
  - "src/pages/definir-senha/**"
  - "src/pages/bem-vindo/**"
  - "src/pages/minha-conta/**"
---

# Decisões — Auth no frontend

Rule irmã: `api/.claude/rules/auth-e-contas.md` — a regra é do backend, a tela só reflete. Codes espelhados em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`.

## Sessão: a arquitetura do boilerplate, sem o gate de painel

`src/api/` (client, interceptor de refresh single-flight, `base-service`, `token-storage` em `localStorage`) veio do boilerplate sem alteração. `auth-provider.tsx` mudou em um ponto: **não exige `Admin.Access`** — qualquer `User` entra na comunidade. `fetchMyAccess` continua sendo chamado (para `can()` nas telas de admin), mas falha dele não derruba a sessão: o `access` fica vazio.

A sessão carrega `ProfileDto { id, name, nickname, email, role, isNeededChangePassword, onboardingCompleted }`. `refreshUser()` é chamado depois do onboarding e depois de editar o perfil.

## Telas e caminhos

| Caminho | Tela | Observação |
|---|---|---|
| `/entrar` | login | e-mail + senha; link para criar conta e para esqueci a senha |
| `/criar-conta` | registro | nome, nickname, e-mail, senha, confirmação; sucesso vira o estado "confira seu e-mail" **na mesma tela**, com botão de reenviar |
| `/confirmar-email?userId&token` | confirmação | chama `confirm-email` na montagem; estados: confirmando / confirmado (botão "Entrar") / inválido (campo de e-mail + "Reenviar") |
| `/esqueci-senha` | pedido de reset | só e-mail; sucesso vira "confira seu e-mail" sem dizer se existe |
| `/redefinir-senha?email&token` | nova senha | senha + confirmação; sucesso → `/entrar` com toast |
| `/definir-senha` | senha inicial (convite) | copiada do boilerplate; só com `isNeededChangePassword` |
| `/bem-vindo` | onboarding | dois passos no mesmo card: boas-vindas + avatar (mock); conclui com `complete-onboarding` |
| `/minha-conta` | conta | nome (real), senha (real), avatar/banner/cidade/gêneros (mock até o módulo Perfil) |

Caminhos ficam em `auth/routes.ts`; ninguém escreve `'/entrar'` em página.

## Gates do `RequireAuth`, nesta ordem

1. `status === 'unknown'` → `AppSkeleton` de tela cheia (nunca redirecionar antes do boot terminar: redirecionaria quem tem sessão válida).
2. Não autenticado → `/entrar` com `state.from` para voltar depois.
3. `isNeededChangePassword` → `/definir-senha` (exceto nela).
4. `!onboardingCompleted` → `/bem-vindo` (exceto nela e em `/minha-conta`, para não prender quem quer só sair).
5. `permission` sem capacidade → `/feed`.

`redirect-if-authenticated.tsx` faz o inverso para `/entrar`, `/criar-conta` e `/esqueci-senha`.

## A única exceção à regra "página não trata erro"

O toast global cobre todo erro de API. **`EMAIL_NOT_CONFIRMED` no login é a exceção**: além do toast, a tela de login mostra uma faixa inline "Seu e-mail ainda não foi confirmado" com o botão "Reenviar e-mail", porque a ação de reenviar é da tela e a pessoa precisa dela ali, não num toast que some. O hook `use-login.ts` captura o `ApiError` por `code` e expõe `pendingConfirmation`. Nenhuma outra tela faz isso sem registrar aqui.

## Validação espelha o Identity

`auth/validators.ts` concentra os schemas zod reutilizados: `passwordSchema` = mínimo 6 e pelo menos um dígito (a política do `AddIdentity`); `nicknameSchema` = `^[a-z0-9](?:[a-z0-9._]{1,28}[a-z0-9])?$` com `transform(lowercase)`; `nameSchema` = 2 a 100; `emailSchema`. ⚠️ Mudou a política no backend, muda aqui no mesmo commit — senão a tela aprova o que o servidor recusa.

## Roteiro de verificação

Com a API e o Mailpit (http://localhost:8025) no ar:

1. `/feed` sem sessão → `/entrar`. Criar conta com nickname `Caio.Teste` → campo normaliza para `caio.teste`; senha `abcdef` → erro inline antes de enviar; válido → estado "confira seu e-mail".
2. Abrir o link do Mailpit → `/confirmar-email` mostra "E-mail confirmado" → "Entrar" → login → `/bem-vindo` → concluir → `/feed`. Recarregar `/feed` mantém a sessão.
3. Login de conta não confirmada → toast + faixa com "Reenviar"; clicar → toast `CONFIRMATION_EMAIL_SENT` e e-mail novo no Mailpit.
4. `/esqueci-senha` com e-mail inexistente → mesma tela de sucesso; com existente → link no Mailpit → `/redefinir-senha` → nova senha → `/entrar` com toast → entrar com a nova.
5. Apagar o `access_token` do `localStorage` e navegar → a request renova pelo refresh e segue sem logout; apagar os dois → `/entrar` com toast `SESSION_EXPIRED` só se havia request em voo.
6. `/minha-conta` → trocar senha com atual errada → toast `INVALID_CREDENTIALS`; certa → toast `PASSWORD_CHANGED` e a sessão continua.
