using Headbanger.Application.BackgroundJobs.Dtos;
using Headbanger.Application.Pagination;
using Headbanger.Application.Response;
using Headbanger.Domain.Enums.Jobs;

namespace Headbanger.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobAppService
    {
        Task<Result<TriggeredBackgroundJobDto>> Trigger(EBackgroundJobType jobType);
        Task<Result<TriggeredBackgroundJobDto>> Retry(int id);
        Task<Result<PagedResultDto<BackgroundJobListItemDto>>> GetRuns(BackgroundJobQueryDto query);
        Task<Result<BackgroundJobDetailDto>> GetRun(int id);
        Task<Result<List<RecurringJobDto>>> GetRecurring();
        Task ExecuteAsync(int runId, CancellationToken ct = default);
    }
}
