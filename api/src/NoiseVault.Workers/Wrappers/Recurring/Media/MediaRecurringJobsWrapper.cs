using Hangfire;
using NoiseVault.Application.Common.Time;
using NoiseVault.Workers.Triggers.Recurring.Media;

namespace NoiseVault.Workers.Wrappers.Recurring.Media
{
    public class MediaRecurringJobsWrapper
    {
        public void Register()
        {
            RecurringJob.AddOrUpdate<MediaRecurringJobsTrigger>(
                recurringJobId: "media:cleanup",
                queue: "media",
                methodCall: trigger => trigger.CleanupAsync(),
                cronExpression: Cron.Hourly(),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );
        }
    }
}
