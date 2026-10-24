using System.Net;
using System.Text;
using NoiseVault.Application.Auth.Dtos;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Interfaces;

namespace NoiseVault.Infra.Data.Email
{
    public class EmailService : IEmailService
    {
        private const string Brand = "#C62828";
        private const string Ink = "#1A1414";
        private const string BodyColor = "#4A4040";
        private const string Muted = "#7A6E6E";
        private const string Faint = "#A39797";
        private const string Outline = "#E8DEDE";
        private const string Page = "#F6F2F2";
        private const string SoftBackground = "#FBEAEA";
        private const string SoftBorder = "#F1C9C9";
        private const string SoftText = "#7A1F1F";
        private const string FontStack = "'Segoe UI', Arial, Helvetica, sans-serif";
        private const string MonoStack = "Consolas, 'Courier New', monospace";

        private readonly IEmailSender _sender;
        private readonly EmailSettings _settings;

        public EmailService(IEmailSender sender, EmailSettings settings)
        {
            _sender = sender;
            _settings = settings;
        }

        public Task<bool> SendEmailConfirmationAsync(EmailConfirmationEmailDto email)
        {
            var url = $"{_settings.FrontendBaseUrl}/confirmar-email?userId={email.UserId}&token={Uri.EscapeDataString(email.Token)}";
            var intro = $"{Greeting(email.Name)} Falta um passo para entrar no Noise Vault: confirme que este e-mail é seu.";

            var html = new StringBuilder()
                .Append(Paragraph(Encode(intro)))
                .Append(Button("Confirmar e-mail", url))
                .Append(Meta("O link vale por 24 horas. Se não foi você quem se cadastrou, ignore esta mensagem."));

            var text = new StringBuilder()
                .AppendLine("Confirme seu e-mail")
                .AppendLine()
                .AppendLine(intro)
                .AppendLine()
                .AppendLine($"Confirmar: {url}")
                .AppendLine()
                .AppendLine("O link vale por 24 horas. Se não foi você quem se cadastrou, ignore esta mensagem.");

            return _sender.SendAsync(
                email.To,
                "Confirme seu e-mail - Noise Vault",
                Layout("Cadastro", "Confirme seu e-mail", html.ToString(), intro, AutomaticFooter()),
                text.ToString());
        }

        public Task<bool> SendPasswordResetAsync(PasswordResetEmailDto email)
        {
            var url = $"{_settings.FrontendBaseUrl}/redefinir-senha?email={Uri.EscapeDataString(email.To)}&token={Uri.EscapeDataString(email.Token)}";
            var intro = $"{Greeting(email.Name)} Recebemos um pedido para redefinir a senha da sua conta no Noise Vault.";

            var html = new StringBuilder()
                .Append(Paragraph(Encode(intro)))
                .Append(Button("Redefinir senha", url))
                .Append(Meta("O link vale por 1 hora. Se não foi você, ignore esta mensagem: sua senha continua a mesma."));

            var text = new StringBuilder()
                .AppendLine("Redefinir senha")
                .AppendLine()
                .AppendLine(intro)
                .AppendLine()
                .AppendLine($"Redefinir: {url}")
                .AppendLine()
                .AppendLine("O link vale por 1 hora. Se não foi você, ignore esta mensagem: sua senha continua a mesma.");

            return _sender.SendAsync(
                email.To,
                "Redefinir senha - Noise Vault",
                Layout("Conta", "Redefinir senha", html.ToString(), intro, AutomaticFooter()),
                text.ToString());
        }

        public Task<bool> SendUserInviteEmail(string email, string name, string roleName, string temporaryPassword)
        {
            var url = $"{_settings.FrontendBaseUrl}/entrar";
            var intro = $"{Greeting(name)} Você foi convidado para a equipe do Noise Vault como {roleName}. Entre com a senha temporária abaixo e defina a sua no primeiro acesso.";

            var html = new StringBuilder()
                .Append(Paragraph(Encode(intro)))
                .Append(Facts(("E-mail", Mono(email)), ("Senha temporária", Mono(temporaryPassword))))
                .Append(Button("Entrar", url));

            var text = new StringBuilder()
                .AppendLine("Convite para a equipe")
                .AppendLine()
                .AppendLine(intro)
                .AppendLine()
                .AppendLine($"E-mail: {email}")
                .AppendLine($"Senha temporária: {temporaryPassword}")
                .AppendLine()
                .AppendLine($"Entrar: {url}");

            return _sender.SendAsync(
                email,
                "Convite para a equipe - Noise Vault",
                Layout("Equipe", "Convite para a equipe", html.ToString(), intro, AutomaticFooter()),
                text.ToString());
        }

        public Task<bool> SendBandInviteAsync(BandInviteEmailDto email)
        {
            var url = $"{_settings.FrontendBaseUrl}/notificacoes";
            var role = string.IsNullOrWhiteSpace(email.Instrument) ? string.Empty : $" para tocar {email.Instrument}";
            var intro = $"{Greeting(email.Name)} {email.InvitedByName} convidou você{role} na {email.BandName}.";
            var expires = $"O convite vale até {AppTime.ToLocal(email.ExpiresAt):dd/MM/yyyy}. Aceite ou recuse na sua página de notificações.";

            var html = new StringBuilder()
                .Append(Paragraph(Encode(intro)))
                .Append(Button("Ver convite", url))
                .Append(Meta(expires));

            var text = new StringBuilder()
                .AppendLine("Convite de banda")
                .AppendLine()
                .AppendLine(intro)
                .AppendLine()
                .AppendLine($"Ver convite: {url}")
                .AppendLine()
                .AppendLine(expires);

            return _sender.SendAsync(
                email.To,
                "Você recebeu um convite de banda - Noise Vault",
                Layout("Bandas", "Convite de banda", html.ToString(), intro, AutomaticFooter()),
                text.ToString());
        }

        public Task<bool> SendBandVerificationRequestedAsync(BandVerificationEmailDto email)
        {
            if (string.IsNullOrWhiteSpace(_settings.VerificationInboxAddress))
                return Task.FromResult(false);

            var bandUrl = $"{_settings.FrontendBaseUrl}{email.BandPath}";
            var subject = email.IsSolo ? "do artista solo" : "da banda";
            var queueUrl = $"{_settings.FrontendBaseUrl}/admin/bandas";
            var intro = $"{email.RequestedByName} (@{email.RequestedByNickname}) pediu a verificação {subject} {email.BandName}.";
            var contact = string.IsNullOrWhiteSpace(email.ContactPhone) ? "Sem telefone de contato." : $"Contato: {email.ContactPhone}.";

            var html = new StringBuilder()
                .Append(Paragraph(Encode(intro)))
                .Append(Paragraph(Encode(email.Message).Replace("\n", "<br />")))
                .Append(Meta(contact))
                .Append(Button("Abrir a fila de verificação", queueUrl))
                .Append(Meta($"Página {subject}: {bandUrl}"));

            var text = new StringBuilder()
                .AppendLine("Pedido de verificação de banda")
                .AppendLine()
                .AppendLine(intro)
                .AppendLine()
                .AppendLine(email.Message)
                .AppendLine()
                .AppendLine(contact)
                .AppendLine()
                .AppendLine($"Fila de verificação: {queueUrl}")
                .AppendLine($"Página {subject}: {bandUrl}");

            return _sender.SendAsync(
                _settings.VerificationInboxAddress,
                "Novo pedido de verificação de banda - Noise Vault",
                Layout("Bandas", "Pedido de verificação", html.ToString(), intro, AutomaticFooter()),
                text.ToString());
        }

        public Task<bool> SendExternalLoginLinkedAsync(ExternalLoginLinkedEmailDto email)
        {
            var url = $"{_settings.FrontendBaseUrl}/minha-conta";
            var intro = $"{Greeting(email.Name)} O {email.ProviderName} foi conectado à sua conta no Noise Vault. A partir de agora você também entra com ele.";
            var warning = $"Se não foi você, abra Minha conta, desconecte o {email.ProviderName} e troque sua senha.";

            var html = new StringBuilder()
                .Append(Paragraph(Encode(intro)))
                .Append(Button("Abrir Minha conta", url))
                .Append(Meta(warning));

            var text = new StringBuilder()
                .AppendLine($"{email.ProviderName} conectado")
                .AppendLine()
                .AppendLine(intro)
                .AppendLine()
                .AppendLine($"Minha conta: {url}")
                .AppendLine()
                .AppendLine(warning);

            return _sender.SendAsync(
                email.To,
                $"{email.ProviderName} conectado à sua conta - Noise Vault",
                Layout("Conta", $"{email.ProviderName} conectado", html.ToString(), intro, AutomaticFooter()),
                text.ToString());
        }

        private static string AutomaticFooter() =>
            "Mensagem automática do Noise Vault, a comunidade de metal e rock do ABC. Não responda.";

        private static string Layout(string eyebrow, string title, string content, string preheader, string footer) => $@"<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
<meta charset=""utf-8"" />
<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
<meta name=""color-scheme"" content=""light"" />
<title>{Encode(title)}</title>
</head>
<body style=""margin:0;padding:0;background:{Page};"">
<div style=""display:none;max-height:0;overflow:hidden;opacity:0;color:{Page};"">{Encode(preheader)}</div>
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background:{Page};"">
<tr><td align=""center"" style=""padding:32px 16px;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:560px;"">
<tr><td style=""background:{Brand};border-radius:12px 12px 0 0;padding:22px 32px;"">
<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr>
<td width=""32"" height=""32"" style=""width:32px;height:32px;background:#FFFFFF;border-radius:8px;text-align:center;vertical-align:middle;font-family:{FontStack};font-weight:700;font-size:17px;color:{Brand};"">H</td>
<td style=""padding-left:10px;font-family:{FontStack};font-weight:700;font-size:18px;letter-spacing:0.04em;text-transform:uppercase;color:#FFFFFF;"">Noise Vault</td>
</tr></table>
</td></tr>
<tr><td style=""background:#FFFFFF;border:1px solid {Outline};border-top:0;border-radius:0 0 12px 12px;padding:32px;font-family:{FontStack};"">
<div style=""font-size:12px;font-weight:600;letter-spacing:0.08em;text-transform:uppercase;color:{Brand};margin:0 0 8px;"">{Encode(eyebrow)}</div>
<h1 style=""margin:0 0 16px;font-size:22px;line-height:1.25;font-weight:700;color:{Ink};"">{Encode(title)}</h1>
{content}
</td></tr>
<tr><td align=""center"" style=""padding:24px 16px 0;font-family:{FontStack};font-size:12px;line-height:1.6;color:{Faint};"">
{Encode(footer)}<br/>&copy; {AppTime.Now.Year} Noise Vault
</td></tr>
</table>
</td></tr>
</table>
</body>
</html>";

        private static string Paragraph(string html) =>
            $"<p style=\"margin:0 0 16px;font-size:15px;line-height:1.6;color:{BodyColor};\">{html}</p>";

        private static string Meta(string text) =>
            $"<p style=\"margin:16px 0 0;font-size:13px;line-height:1.5;color:{Muted};\">{Encode(text)}</p>";

        private static string Button(string label, string href) =>
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:8px 0 0;\"><tr>" +
            $"<td style=\"background:{Brand};border-radius:8px;\"><a href=\"{Encode(href)}\" style=\"display:inline-block;padding:12px 22px;font-family:{FontStack};font-size:15px;font-weight:600;color:#FFFFFF;text-decoration:none;\">{Encode(label)}</a></td>" +
            "</tr></table>";

        private static string Facts(params (string Label, string ValueHtml)[] rows)
        {
            var body = new StringBuilder();

            for (var i = 0; i < rows.Length; i++)
            {
                var border = i == 0 ? string.Empty : $"border-top:1px solid {Outline};";
                body.Append($"<tr><td valign=\"top\" style=\"{border}padding:10px 14px;width:150px;font-size:14px;color:{Muted};\">{Encode(rows[i].Label)}</td>");
                body.Append($"<td style=\"{border}padding:10px 14px;font-size:14px;font-weight:500;color:{Ink};\">{rows[i].ValueHtml}</td></tr>");
            }

            return $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 0 20px;border:1px solid {Outline};border-radius:10px;border-collapse:separate;background:{SoftBackground};border-color:{SoftBorder};color:{SoftText};\">{body}</table>";
        }

        private static string Mono(string value) =>
            $"<span style=\"font-family:{MonoStack};font-size:13px;\">{Encode(value)}</span>";

        private static string Greeting(string name)
        {
            var first = FirstName(name);
            return first.Length == 0 ? "Olá." : $"Olá, {first}.";
        }

        private static string FirstName(string name)
        {
            var trimmed = name?.Trim() ?? string.Empty;
            var space = trimmed.IndexOf(' ');
            return space > 0 ? trimmed[..space] : trimmed;
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
