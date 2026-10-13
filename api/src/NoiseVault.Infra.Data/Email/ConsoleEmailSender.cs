using NoiseVault.Application.Abstractions.Logging;

namespace NoiseVault.Infra.Data.Email
{
    public class ConsoleEmailSender : IEmailSender
    {
        private readonly IApplicationLogger _logger;

        public ConsoleEmailSender(IApplicationLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.Create(LogContext.Email);
        }

        public Task<bool> SendAsync(string to, string subject, string html, string text)
        {
            _logger.Info(
                eventName: "EMAIL_STUBBED",
                message: "E-mail delivery is disabled (Email:Provider is not Smtp); nothing was sent",
                extra: new { Subject = subject, TextLength = text.Length });

            return Task.FromResult(true);
        }
    }
}
