using Hangfire;
using Hangfire.Storage;
using ApplicationRecurringJobDto = Headbanger.Application.BackgroundJobs.Dtos.RecurringJobDto;
using Headbanger.Application.BackgroundJobs.Interfaces;

namespace Headbanger.Workers.Monitoring
{
    public class RecurringJobReader : IRecurringJobReader
    {
        public IReadOnlyList<ApplicationRecurringJobDto> GetRecurringJobs()
        {
            using var connection = JobStorage.Current.GetConnection();

            return connection
                .GetRecurringJobs()
                .Select(job => new ApplicationRecurringJobDto
                {
                    Id = job.Id,
                    Cron = job.Cron,
                    Queue = job.Job?.Queue ?? job.Queue,
                    LastExecution = job.LastExecution,
                    NextExecution = job.NextExecution,
                    LastJobState = job.LastJobState,
                })
                .OrderBy(job => job.Id, StringComparer.Ordinal)
                .ToList();
        }
    }
}
