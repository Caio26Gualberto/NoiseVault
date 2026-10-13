using Headbanger.Application.Abstractions.Repository;
using Headbanger.Domain.Entities;

namespace Headbanger.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobRunRepository : IRepository<BackgroundJobRun>
    {
        Task<bool> TryClaimAsync(int runId, CancellationToken ct = default);
        Task ReleaseDeduplicationKeyAsync(int runId, CancellationToken ct = default);
    }
}
