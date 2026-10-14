using Hangfire;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Notifications.Interfaces;

namespace NoiseVault.Workers.Triggers.Recurring.Notifications
{
    public class NotificationsRecurringJobsTrigger
    {
        private readonly IEventAppService _eventAppService;
        private readonly INotificationAppService _notificationAppService;

        public NotificationsRecurringJobsTrigger(IEventAppService eventAppService, INotificationAppService notificationAppService)
        {
            _eventAppService = eventAppService;
            _notificationAppService = notificationAppService;
        }

        [Queue("notifications")]
        [JobDisplayName("Notificações: lembrar shows de amanhã")]
        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(timeoutInSeconds: 30)]
        public Task RemindEventsTomorrowAsync() =>
            _eventAppService.RemindTomorrow();

        [Queue("notifications")]
        [JobDisplayName("Notificações: apagar antigas")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 600 })]
        [DisableConcurrentExecution(timeoutInSeconds: 30)]
        public Task PurgeAsync() =>
            _notificationAppService.Purge();
    }
}
