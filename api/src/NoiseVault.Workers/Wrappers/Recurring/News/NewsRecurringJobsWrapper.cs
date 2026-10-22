using Hangfire;
using NoiseVault.Application.Common.Time;
using NoiseVault.Workers.Triggers.Recurring.News;

namespace NoiseVault.Workers.Wrappers.Recurring.News
{
    public class NewsRecurringJobsWrapper
    {
        public void Register()
        {
            RecurringJob.AddOrUpdate<NewsRecurringJobsTrigger>(
                recurringJobId: "news:collect",
                queue: "default",
                methodCall: trigger => trigger.CollectAsync(),
                cronExpression: Cron.Hourly(15),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );

            RecurringJob.AddOrUpdate<NewsRecurringJobsTrigger>(
                recurringJobId: "news:purge",
                queue: "default",
                methodCall: trigger => trigger.PurgeAsync(),
                cronExpression: Cron.Daily(4),
                options: new RecurringJobOptions { TimeZone = AppTime.Zone }
            );
        }
    }
}
