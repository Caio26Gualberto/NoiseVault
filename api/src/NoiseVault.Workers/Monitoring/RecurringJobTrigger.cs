using Hangfire;
using NoiseVault.Application.BackgroundJobs.Interfaces;

namespace NoiseVault.Workers.Monitoring
{
    public class RecurringJobTrigger : IRecurringJobTrigger
    {
        public string? Trigger(string recurringJobId) => RecurringJob.TriggerJob(recurringJobId);
    }
}
