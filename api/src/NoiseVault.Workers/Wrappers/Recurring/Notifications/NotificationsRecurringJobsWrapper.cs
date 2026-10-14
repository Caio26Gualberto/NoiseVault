using Hangfire;
using NoiseVault.Application.Common.Time;
using NoiseVault.Workers.Triggers.Recurring.Notifications;

namespace NoiseVault.Workers.Wrappers.Recurring.Notifications
{
    public class NotificationsRecurringJobsWrapper
    {
        public void Register()
        {
            RecurringJob.AddOrUpdate<NotificationsRecurringJobsTrigger>(
                recurringJobId: "events:remind-tomorrow",
                queue: "notifications",
                methodCall: trigger => trigger.RemindEventsTomorrowAsync(),
                cronExpression: Cron.Daily(10),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );

            RecurringJob.AddOrUpdate<NotificationsRecurringJobsTrigger>(
                recurringJobId: "notifications:purge",
                queue: "notifications",
                methodCall: trigger => trigger.PurgeAsync(),
                cronExpression: Cron.Daily(4),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );
        }
    }
}
