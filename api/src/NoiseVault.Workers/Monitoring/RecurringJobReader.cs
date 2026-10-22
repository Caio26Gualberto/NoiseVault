using System.Globalization;
using System.Reflection;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using ApplicationRecurringJobDto = NoiseVault.Application.BackgroundJobs.Dtos.RecurringJobDto;
using ApplicationFailedJobDto = NoiseVault.Application.BackgroundJobs.Dtos.FailedJobDto;
using NoiseVault.Application.BackgroundJobs.Interfaces;

namespace NoiseVault.Workers.Monitoring
{
    public class RecurringJobReader : IRecurringJobReader
    {
        private const string RecurringJobIdParameter = "RecurringJobId";

        public IReadOnlyList<ApplicationRecurringJobDto> GetRecurringJobs()
        {
            var monitoring = JobStorage.Current.GetMonitoringApi();
            using var connection = JobStorage.Current.GetConnection();

            return connection
                .GetRecurringJobs()
                .Select(job =>
                {
                    var dto = new ApplicationRecurringJobDto
                    {
                        Id = job.Id,
                        Name = DisplayName(job.Job),
                        Cron = job.Cron,
                        Queue = job.Job?.Queue ?? job.Queue,
                        LastExecution = job.LastExecution,
                        NextExecution = job.NextExecution,
                        LastJobId = job.LastJobId,
                        LastJobState = job.LastJobState,
                        Error = job.Error,
                    };

                    if (!string.IsNullOrEmpty(job.LastJobId))
                        FillLastRun(dto, monitoring.JobDetails(job.LastJobId));

                    return dto;
                })
                .OrderBy(job => job.Id, StringComparer.Ordinal)
                .ToList();
        }

        public (IReadOnlyList<ApplicationFailedJobDto> Items, int TotalCount) GetFailedJobs(int skip, int take)
        {
            var monitoring = JobStorage.Current.GetMonitoringApi();
            using var connection = JobStorage.Current.GetConnection();

            var total = (int)Math.Min(int.MaxValue, monitoring.FailedCount());
            var items = monitoring
                .FailedJobs(skip, take)
                .Select(entry => new ApplicationFailedJobDto
                {
                    JobId = entry.Key,
                    Name = DisplayName(entry.Value?.Job),
                    RecurringJobId = connection.GetJobParameter(entry.Key, RecurringJobIdParameter)?.Trim('"'),
                    Queue = entry.Value?.Job?.Queue,
                    FailedAt = entry.Value?.FailedAt,
                    ExceptionType = entry.Value?.ExceptionType,
                    ExceptionMessage = entry.Value?.ExceptionMessage,
                    ExceptionDetails = entry.Value?.ExceptionDetails,
                })
                .ToList();

            return (items, total);
        }

        private static void FillLastRun(ApplicationRecurringJobDto dto, JobDetailsDto? details)
        {
            if (details?.History == null)
                return;

            var current = details.History.FirstOrDefault();

            if (current == null)
                return;

            if (current.StateName == SucceededState.StateName)
            {
                dto.LastResult = Value(current.Data, "Result");

                if (long.TryParse(Value(current.Data, "PerformanceDuration"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var duration))
                    dto.LastDurationMs = duration;
            }

            var failure = details.History.FirstOrDefault(state => state.StateName == FailedState.StateName);

            if (failure != null && current.StateName != SucceededState.StateName)
            {
                dto.LastError = Value(failure?.Data, "ExceptionMessage");
                dto.LastErrorDetails = Value(failure?.Data, "ExceptionDetails");
            }
        }

        private static string? Value(IDictionary<string, string>? data, string key) =>
            data != null && data.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : null;

        private static string? DisplayName(Job? job) =>
            job?.Method.GetCustomAttribute<JobDisplayNameAttribute>()?.DisplayName;
    }
}
