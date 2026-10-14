---
paths:
  - "**/NoiseVault.Infra.Data/Email/**"
  - "**/NoiseVault.Application/Interfaces/IEmailService.cs"
  - "**/NoiseVault.Application/Auth/Dtos/*EmailDto.cs"
---

# Decisões — E-mail

Hoje o produto manda quatro e-mails: **confirmação de cadastro**, **redefinição de senha**, **convite para a equipe** e **convite de banda** (`SendBandInviteAsync`, módulo 6). Todos são transacionais, um destinatário, um botão. O convite de banda é o único aviso da comunidade que vai por e-mail, por decisão do usuário: é o único em que alguém precisa agir. Sai por job na fila `notifications`, respeita `User.EmailOnBandInvite` e tem assunto sem nome ("Você recebeu um convite de banda") (`notificacoes.md`).

## Duas camadas: o que dizer e como mandar

- **`IEmailService`** (`Application/Interfaces/`) é tipado por caso de uso: `SendEmailConfirmationAsync(EmailConfirmationEmailDto)`, `SendPasswordResetAsync(PasswordResetEmailDto)`, `SendUserInviteEmail(...)`. Recebe **dados** (nome, destinatário, token), nunca HTML nem URL — a Application não sabe de layout nem de onde o site está hospedado. Devolve `false` quando o transporte não aceitou; quem chama decide o que fazer com isso (`auth-e-contas.md`).
- **`EmailService`** (`Infra.Data/Email/`) monta assunto, HTML e versão texto, e monta os links a partir de `Frontend:BaseUrl`. Layout herdado do Buscaprocesso: tabela de 560 px, cabeçalho na cor da marca, preheader, um botão, rodapé dizendo por que a pessoa recebe. `<meta name="color-scheme" content="light">` de propósito: e-mail em modo escuro é imprevisível entre clientes e o site já é escuro — o e-mail é o lugar claro.
- **`IEmailSender`** é o transporte: `SmtpEmailSender` (MailKit) ou `ConsoleEmailSender`. `Email:Provider` (`Smtp` | qualquer outro valor) escolhe no `AddInfra`, e o boot avisa quando cai no console.

## Dev: Mailpit no docker-compose, SMTP como denominador comum

- O compose sobe `axllent/mailpit` (`noisevault-mailpit`): SMTP em `localhost:1025` sem TLS nem autenticação, caixa de entrada web em **http://localhost:8025**. `appsettings.Development.json` já aponta para ele com `Email:Provider=Smtp`.
- Escolha: SMTP genérico em vez de emular um provedor específico (SES, Resend) porque **o provedor de produção ainda não foi escolhido**. Qualquer um deles fala SMTP; quando a escolha vier com API própria, entra um segundo `IEmailSender` e o `SmtpEmailSender` continua servindo o dev.
- `SmtpEmailSender`: `SecureSocketOptions.StartTls` quando `Email:Smtp:UseStartTls=true`, `None` caso contrário (Mailpit); autentica só se `Email:Smtp:Username` estiver preenchido; timeout de 10 s. Exceção vira `EMAIL_SEND_FAILED` (Warning) e `false` — nunca sobe para o request.
- Produção: `Email__Smtp__Host/Port/Username/Password` por variável de ambiente; `Email:From` fica no `appsettings.json` porque não é segredo.

## Links: o front recebe `userId`+`token` ou `email`+`token`

- Confirmação: `{Frontend:BaseUrl}/confirmar-email?userId={id}&token={base64url}`.
- Redefinição: `{Frontend:BaseUrl}/redefinir-senha?email={urlencoded}&token={base64url}`.

🔴 **O `email` na URL de redefinição é a única exceção à regra "dado pessoal nunca em URL" do monorepo.** O `UserManager.ResetPasswordAsync` precisa achar a conta; `userId` sequencial na URL permitiria enumerar contas e cruzar com o token; um token opaco próprio (tabela nossa, lookup por hash) resolveria os dois, mas é uma segunda implementação de token com o Identity já tendo uma. Risco aceito em 13/10/2026: o link vive 1 h, só chega na caixa do próprio dono, e o front não loga query string. Se o projeto ganhar token opaco próprio por outro motivo, esta exceção morre.

- **Todo valor vai por `WebUtility.HtmlEncode`** no HTML e `Uri.EscapeDataString` na query string. O nome do usuário é texto de terceiro.
- O botão e o link em texto puro levam a mesma URL; a versão texto existe para cliente sem HTML e para o Mailpit mostrar algo legível.

## Dado pessoal e segredo no log

- **Nenhum log de e-mail carrega destinatário, link ou token.** `EMAIL_SENT` leva `MessageId` e assunto; `EMAIL_SEND_FAILED`, assunto e tipo da exceção; `EMAIL_STUBBED`, assunto e tamanho do texto. O assunto nunca contém nome nem e-mail.
- Em dev, o lugar de ver o conteúdo é o Mailpit, não o stdout. O `ConsoleEmailSender` existe para subir a API sem Docker, não para ler e-mail.

## Eventos de log (contexto `Email`)

| Evento | Nível | Quando |
|---|---|---|
| `EMAIL_SENT` | Info | SMTP aceitou: `MessageId`, `Subject` |
| `EMAIL_SEND_FAILED` | Warning | conexão, autenticação ou envio falharam; devolve `false` |
| `EMAIL_STUBBED` | Info | `Email:Provider` não é `Smtp`; nada foi enviado |

## Roteiro de verificação

1. `docker compose up -d` → http://localhost:8025 abre vazio.
2. API com `Email:Provider=Smtp` (dev) → boot sem aviso de stub; `POST /api/auth/register` → `EMAIL_SENT` no log com `MessageId` e sem endereço; o e-mail aparece no Mailpit com cabeçalho vermelho, botão "Confirmar e-mail" e a versão texto com a mesma URL.
3. Clicar o botão abre `http://localhost:5173/confirmar-email?userId=…&token=…`.
4. `POST /api/auth/forgot-password` → e-mail "Redefinir senha" com `/redefinir-senha?email=…&token=…`.
5. Parar o Mailpit (`docker compose stop mailpit`) e registrar de novo → `EMAIL_SEND_FAILED` (Warning) + `EMAIL_CONFIRMATION_SEND_FAILED` no contexto Auth, e a resposta continua `REGISTRATION_PENDING_CONFIRMATION`.
6. `Email:Provider=Console` → aviso no boot, `EMAIL_STUBBED` por envio, nada no Mailpit.
