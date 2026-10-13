using Headbanger.Application.Abstractions.Logging;
using Headbanger.Application.Interfaces;

namespace Headbanger.Infra.Data.Email
{
    public class ConsoleEmailService : IEmailService
    {
        private readonly IApplicationLogger _logger;

        public ConsoleEmailService(IApplicationLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.Create(LogContext.Email);
        }

        public Task<bool> SendUserInviteEmail(string email, string name, string roleName, string temporaryPassword)
        {
            _logger.Info(
                eventName: "EMAIL_USER_INVITE",
                message: "E-mail delivery is stubbed; replace ConsoleEmailService with a real provider",
                extra: new { To = email, Name = name, Role = roleName, TemporaryPassword = temporaryPassword });

            return Task.FromResult(true);
        }
    }
}
