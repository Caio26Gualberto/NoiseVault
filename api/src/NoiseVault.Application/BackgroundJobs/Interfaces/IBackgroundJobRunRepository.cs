using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobRunRepository : IRepository<BackgroundJobRun>
    {
        Task<bool> TryClaimAsync(int runId, CancellationToken ct = default);
        Task ReleaseDeduplicationKeyAsync(int runId, CancellationToken ct = default);
    }
}
