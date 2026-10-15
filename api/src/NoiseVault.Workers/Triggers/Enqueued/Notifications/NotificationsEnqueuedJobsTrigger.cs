using Hangfire;
using NoiseVault.Application.Bands.Interfaces;

namespace NoiseVault.Workers.Triggers.Enqueued.Notifications
{
    public class NotificationsEnqueuedJobsTrigger
    {
        private readonly IBandInvitationAppService _invitationAppService;
        private readonly IBandVerificationAppService _verificationAppService;

        public NotificationsEnqueuedJobsTrigger(IBandInvitationAppService invitationAppService, IBandVerificationAppService verificationAppService)
        {
            _invitationAppService = invitationAppService;
            _verificationAppService = verificationAppService;
        }

        [Queue("notifications")]
        [JobDisplayName("Notificações: e-mail do convite de banda {0}")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 600 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public Task SendBandInviteEmailAsync(int invitationId) =>
            _invitationAppService.SendInviteEmail(invitationId);

        [Queue("notifications")]
        [JobDisplayName("Notificações: e-mail do pedido de verificação de banda {0}")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 600 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public Task SendBandVerificationEmailAsync(int requestId) =>
            _verificationAppService.SendRequestedEmail(requestId);
    }
}
