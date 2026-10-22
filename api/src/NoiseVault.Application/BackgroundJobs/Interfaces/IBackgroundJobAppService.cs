using NoiseVault.Application.BackgroundJobs.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Enums.Jobs;

namespace NoiseVault.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobAppService
    {
        Task<Result<TriggeredBackgroundJobDto>> Trigger(EBackgroundJobType jobType);
        Task<Result<TriggeredBackgroundJobDto>> Retry(int id);
        Task<Result<PagedResultDto<BackgroundJobListItemDto>>> GetRuns(BackgroundJobQueryDto query);
        Task<Result<BackgroundJobDetailDto>> GetRun(int id);
        Task<Result<List<RecurringJobDto>>> GetRecurring();
        Task<Result<TriggeredRecurringJobDto>> TriggerRecurring(string id);
        Task<Result<PagedResultDto<FailedJobDto>>> GetFailures(PagedRequestDto query);
        Task ExecuteAsync(int runId, CancellationToken ct = default);
    }
}
