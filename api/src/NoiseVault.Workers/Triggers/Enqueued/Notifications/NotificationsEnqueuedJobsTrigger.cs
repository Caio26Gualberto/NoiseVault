using Hangfire;
using NoiseVault.Application.Bands.Interfaces;

namespace NoiseVault.Workers.Triggers.Enqueued.Notifications
{
    public class NotificationsEnqueuedJobsTrigger
    {
        private readonly IBandInvitationAppService _invitationAppService;

        public NotificationsEnqueuedJobsTrigger(IBandInvitationAppService invitationAppService)
        {
            _invitationAppService = invitationAppService;
        }

        [Queue("notifications")]
        [JobDisplayName("Notificações: e-mail do convite de banda {0}")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 600 }, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public Task SendBandInviteEmailAsync(int invitationId) =>
            _invitationAppService.SendInviteEmail(invitationId);
    }
}
