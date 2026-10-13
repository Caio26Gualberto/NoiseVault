using NoiseVault.Application.BackgroundJobs.Dtos;
using NoiseVault.Domain.Enums.Jobs;

namespace NoiseVault.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobHandler
    {
        EBackgroundJobType JobType { get; }
        Task<BackgroundJobExecutionResultDto> ExecuteAsync(string? payloadJson, CancellationToken ct = default);
    }
}
