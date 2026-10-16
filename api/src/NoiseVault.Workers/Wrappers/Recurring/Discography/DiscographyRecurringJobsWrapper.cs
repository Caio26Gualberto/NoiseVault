using Hangfire;
using NoiseVault.Application.Common.Time;
using NoiseVault.Workers.Triggers.Recurring.Discography;

namespace NoiseVault.Workers.Wrappers.Recurring.Discography
{
    public class DiscographyRecurringJobsWrapper
    {
        public void Register()
        {
            RecurringJob.AddOrUpdate<DiscographyRecurringJobsTrigger>(
                recurringJobId: "discography:refresh",
                queue: "default",
                methodCall: trigger => trigger.RefreshAsync(),
                cronExpression: Cron.Daily(5),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );
        }
    }
}
