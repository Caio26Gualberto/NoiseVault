using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Jobs;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.BackgroundJobs.Dtos;
using NoiseVault.Application.BackgroundJobs.Interfaces;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Jobs;

namespace NoiseVault.Application.BackgroundJobs
{
    public class BackgroundJobAppService : IBackgroundJobAppService
    {
        private readonly IBackgroundJobRunRepository _repository;
        private readonly IEnqueuedJobDispatcher _dispatcher;
        private readonly IRecurringJobReader _recurringJobReader;
        private readonly IRecurringJobTrigger _recurringJobTrigger;
        private readonly IEnumerable<IBackgroundJobHandler> _handlers;
        private readonly ICurrentUser _currentUser;
        private readonly IApplicationLogger _logger;

        public BackgroundJobAppService(
            IBackgroundJobRunRepository repository,
            IEnqueuedJobDispatcher dispatcher,
            IRecurringJobReader recurringJobReader,
            IRecurringJobTrigger recurringJobTrigger,
            IEnumerable<IBackgroundJobHandler> handlers,
            ICurrentUser currentUser,
            IApplicationLoggerFactory loggerFactory)
        {
            _repository = repository;
            _dispatcher = dispatcher;
            _recurringJobReader = recurringJobReader;
            _recurringJobTrigger = recurringJobTrigger;
            _handlers = handlers;
            _currentUser = currentUser;
            _logger = loggerFactory.Create(LogContext.BackgroundJobs);
        }

        public Task<Result<TriggeredBackgroundJobDto>> Trigger(EBackgroundJobType jobType) =>
            CreateAndEnqueueAsync(jobType, payloadJson: null, DeduplicationKey(jobType));

        public async Task<Result<TriggeredBackgroundJobDto>> Retry(int id)
        {
            var original = await _repository.FirstOrDefaultAsync(new QueryOptions<BackgroundJobRun>
            {
                AsNoTracking = true,
                Predicate = j => j.Id == id && j.Status == EBackgroundJobStatus.Failed,
            });

            if (original == null)
                return Result<TriggeredBackgroundJobDto>.Fail(
                    new Error("BACKGROUND_JOB_NOT_RETRYABLE", "Only failed background jobs can be retried", ErrorType.Validation));

            return await CreateAndEnqueueAsync(original.JobType, original.PayloadJson, DeduplicationKey(original.JobType));
        }

        public async Task<Result<PagedResultDto<BackgroundJobListItemDto>>> GetRuns(BackgroundJobQueryDto query)
        {
            query.Normalize();

            var fromUtc = AppTime.DayStartUtc(query.From);
            var toUtc = AppTime.NextDayStartUtc(query.To);

            var options = new QueryOptions<BackgroundJobRun>
            {
                AsNoTracking = true,
                Includes = [j => j.RequestedByUser!],
                Predicate = j =>
                    (query.JobType == null || j.JobType == query.JobType)
                    && (query.Status == null || j.Status == query.Status)
                    && (fromUtc == null || j.CreatedAt >= fromUtc)
                    && (toUtc == null || j.CreatedAt < toUtc),
                OrderBy = q => q.OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id),
                Skip = query.Skip,
                Take = query.PageSize,
            };

            var totalCount = await _repository.CountAsync(options);
            var runs = await _repository.ListAsync(options);
            var items = runs.Select(ToListItem).ToList();

            var page = PagedResultDto<BackgroundJobListItemDto>.Create(items, query.Page, query.PageSize, totalCount);

            return Result<PagedResultDto<BackgroundJobListItemDto>>.Ok(page, "BACKGROUND_JOBS_LOADED", "Background jobs loaded successfully.");
        }

        public async Task<Result<BackgroundJobDetailDto>> GetRun(int id)
        {
            var run = await _repository.FirstOrDefaultAsync(new QueryOptions<BackgroundJobRun>
            {
                AsNoTracking = true,
                Includes = [j => j.RequestedByUser!],
                Predicate = j => j.Id == id,
            });

            if (run == null)
                return Result<BackgroundJobDetailDto>.Fail(
                    new Error("BACKGROUND_JOB_NOT_FOUND", "Background job not found", ErrorType.NotFound));

            var dto = new BackgroundJobDetailDto
            {
                PayloadJson = run.PayloadJson,
                ResultJson = run.ResultJson,
                HangfireJobId = run.HangfireJobId,
            };

            CopyListItem(run, dto);

            return Result<BackgroundJobDetailDto>.Ok(dto, "BACKGROUND_JOB_LOADED", "Background job loaded successfully.");
        }

        public Task<Result<List<RecurringJobDto>>> GetRecurring()
        {
            var jobs = _recurringJobReader.GetRecurringJobs().ToList();

            foreach (var job in jobs)
                job.CanTrigger = !JobRules.NotTriggerable.Contains(job.Id);

            return Task.FromResult(Result<List<RecurringJobDto>>.Ok(jobs, "RECURRING_JOBS_LOADED", "Recurring jobs loaded successfully."));
        }

        public Task<Result<TriggeredRecurringJobDto>> TriggerRecurring(string id)
        {
            _logger.SetOperation("TriggerRecurringJob");
            _logger.Set("RecurringJobId", id);

            var job = _recurringJobReader.GetRecurringJobs().FirstOrDefault(j => j.Id == id);

            if (job == null)
                return Task.FromResult(Result<TriggeredRecurringJobDto>.Fail(new Error("RECURRING_JOB_NOT_FOUND", "Recurring job not found", ErrorType.NotFound)));

            if (JobRules.NotTriggerable.Contains(job.Id))
                return Task.FromResult(Result<TriggeredRecurringJobDto>.Fail(new Error("JOB_TRIGGER_NOT_ALLOWED", "This recurring job cannot be triggered manually", ErrorType.Conflict)));

            if (job.LastJobState != null && JobRules.RunningStates.Contains(job.LastJobState))
                return Task.FromResult(Result<TriggeredRecurringJobDto>.Fail(new Error("JOB_ALREADY_RUNNING", "This job is already queued or running", ErrorType.Conflict)));

            var jobId = _recurringJobTrigger.Trigger(job.Id);

            if (string.IsNullOrEmpty(jobId))
                return Task.FromResult(Result<TriggeredRecurringJobDto>.Fail(new Error("RECURRING_JOB_NOT_FOUND", "Recurring job not found", ErrorType.NotFound)));

            _logger.Info("RECURRING_JOB_TRIGGERED", "Recurring job triggered manually", userId: _currentUser.UserIdOrNull, extra: new { RecurringJobId = job.Id, JobId = jobId });

            return Task.FromResult(Result<TriggeredRecurringJobDto>.Ok(
                new TriggeredRecurringJobDto { RecurringJobId = job.Id, JobId = jobId },
                "RECURRING_JOB_TRIGGERED",
                "Recurring job triggered"));
        }

        public Task<Result<PagedResultDto<FailedJobDto>>> GetFailures(PagedRequestDto query)
        {
            query.Normalize();

            var (items, total) = _recurringJobReader.GetFailedJobs(query.Skip, query.PageSize);

            return Task.FromResult(Result<PagedResultDto<FailedJobDto>>.Ok(
                PagedResultDto<FailedJobDto>.Create(items.ToList(), query.Page, query.PageSize, total),
                "FAILED_JOBS_FOUND",
                "Failed jobs found"));
        }

        public async Task ExecuteAsync(int runId, CancellationToken ct = default)
        {
            _logger.SetOperation("ExecuteBackgroundJob");
            _logger.Set("RunId", runId);

            if (!await _repository.TryClaimAsync(runId, ct))
                return;

            var run = await _repository.GetByIdAsync(runId);

            if (run == null)
                return;

            try
            {
                var handler = _handlers.FirstOrDefault(h => h.JobType == run.JobType)
                    ?? throw new InvalidOperationException($"Nenhum handler registrado para o job {run.JobType}.");

                var result = await handler.ExecuteAsync(run.PayloadJson, ct);

                run.Status = EBackgroundJobStatus.Completed;
                run.ResultJson = result.ResultJson;
                run.ResultStorageKey = result.ResultStorageKey;
            }
            catch (Exception ex)
            {
                run.Status = EBackgroundJobStatus.Failed;
                run.ErrorMessage = Truncate(ex.Message, 2000);

                _logger.Error(
                    eventName: "BACKGROUND_JOB_FAILED",
                    message: "Background job handler threw",
                    exception: ex,
                    userId: run.RequestedByUserId,
                    extra: new { run.JobType });
            }

            run.CompletedAt = DateTime.UtcNow;
            run.DeduplicationKey = null;

            await _repository.UpdateAsync(run);
        }

        private async Task<Result<TriggeredBackgroundJobDto>> CreateAndEnqueueAsync(
            EBackgroundJobType jobType,
            string? payloadJson,
            string deduplicationKey)
        {
            var inFlight = await _repository.CountAsync(new QueryOptions<BackgroundJobRun>
            {
                AsNoTracking = true,
                Predicate = j => j.DeduplicationKey == deduplicationKey,
            });

            if (inFlight > 0)
                return Result<TriggeredBackgroundJobDto>.Fail(
                    new Error("JOB_ALREADY_RUNNING", "A job of this type is already running", ErrorType.Conflict));

            var run = new BackgroundJobRun
            {
                JobType = jobType,
                Status = EBackgroundJobStatus.Pending,
                RequestedByUserId = _currentUser.UserIdOrNull,
                RequestedByEmail = _currentUser.UserIdOrNull == null ? null : _currentUser.Email,
                PayloadJson = payloadJson,
                DeduplicationKey = deduplicationKey,
            };

            await _repository.AddAsync(run);

            try
            {
                run.HangfireJobId = _dispatcher.EnqueueBackgroundJobRun(run.Id, jobType);
                await _repository.UpdateAsync(run);
            }
            catch (Exception ex)
            {
                run.Status = EBackgroundJobStatus.Failed;
                run.ErrorMessage = Truncate(ex.Message, 2000);
                run.CompletedAt = DateTime.UtcNow;
                run.DeduplicationKey = null;
                await _repository.UpdateAsync(run);

                _logger.Error(
                    eventName: "BACKGROUND_JOB_DISPATCH_FAILED",
                    message: "Background job could not be enqueued",
                    exception: ex,
                    userId: run.RequestedByUserId,
                    extra: new { run.JobType, RunId = run.Id });

                return Result<TriggeredBackgroundJobDto>.Fail(
                    new Error("BACKGROUND_JOB_DISPATCH_FAILED", "The background job could not be dispatched", ErrorType.Unexpected));
            }

            var dto = new TriggeredBackgroundJobDto
            {
                Id = run.Id,
                JobType = run.JobType,
                Status = run.Status,
                CreatedAt = run.CreatedAt,
            };

            return Result<TriggeredBackgroundJobDto>.Ok(dto, "JOB_TRIGGERED", "Job triggered successfully.");
        }

        private static string DeduplicationKey(EBackgroundJobType jobType) => $"{(int)jobType}";

        private static BackgroundJobListItemDto ToListItem(BackgroundJobRun run)
        {
            var dto = new BackgroundJobListItemDto();
            CopyListItem(run, dto);
            return dto;
        }

        private static void CopyListItem(BackgroundJobRun run, BackgroundJobListItemDto dto)
        {
            dto.Id = run.Id;
            dto.JobType = run.JobType;
            dto.Status = run.Status;
            dto.RequestedByUserId = run.RequestedByUserId;
            dto.RequestedByName = run.RequestedByUser?.Name;
            dto.RequestedByEmail = run.RequestedByEmail;
            dto.HasFile = !string.IsNullOrWhiteSpace(run.ResultStorageKey);
            dto.ErrorMessage = run.ErrorMessage;
            dto.StartedAt = run.StartedAt;
            dto.CompletedAt = run.CompletedAt;
            dto.CreatedAt = run.CreatedAt;
            dto.DurationSeconds = run.StartedAt.HasValue && run.CompletedAt.HasValue
                ? (int)Math.Round((run.CompletedAt.Value - run.StartedAt.Value).TotalSeconds)
                : null;
        }

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
