using Headbanger.Application.BackgroundJobs.Dtos;
using Headbanger.Domain.Enums.Jobs;

namespace Headbanger.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobHandler
    {
        EBackgroundJobType JobType { get; }
        Task<BackgroundJobExecutionResultDto> ExecuteAsync(string? payloadJson, CancellationToken ct = default);
    }
}
