using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NoiseVault.Application.Abstractions.Logging;

namespace NoiseVault.Infra.Data.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private const int TimeoutMilliseconds = 10_000;

        private readonly EmailSettings _settings;
        private readonly IApplicationLogger _logger;

        public SmtpEmailSender(EmailSettings settings, IApplicationLoggerFactory loggerFactory)
        {
            _settings = settings;
            _logger = loggerFactory.Create(LogContext.Email);
        }

        public async Task<bool> SendAsync(string to, string subject, string html, string text)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = html, TextBody = text }.ToMessageBody();

            try
            {
                using var client = new SmtpClient { Timeout = TimeoutMilliseconds };

                await client.ConnectAsync(
                    _settings.SmtpHost,
                    _settings.SmtpPort,
                    _settings.SmtpUseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                if (!string.IsNullOrEmpty(_settings.SmtpUsername))
                    await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(quit: true);

                _logger.Info(
                    eventName: "EMAIL_SENT",
                    message: "E-mail accepted by the SMTP server",
                    extra: new { message.MessageId, Subject = subject });

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    eventName: "EMAIL_SEND_FAILED",
                    message: "SMTP delivery failed",
                    extra: new { Subject = subject, Error = ex.GetType().Name, ex.Message });

                return false;
            }
        }
    }
}
