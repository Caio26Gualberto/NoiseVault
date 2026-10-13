using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.BackgroundJobs.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Jobs;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.BackgroundJobRuns
{
    public class BackgroundJobRunRepository : Repository<BackgroundJobRun>, IBackgroundJobRunRepository
    {
        public BackgroundJobRunRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> TryClaimAsync(int runId, CancellationToken ct = default)
        {
            var startedAt = DateTime.UtcNow;

            var affected = await _dbSet
                .Where(j => j.Id == runId && j.Status == EBackgroundJobStatus.Pending)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(j => j.Status, EBackgroundJobStatus.Running)
                        .SetProperty(j => j.StartedAt, startedAt),
                    ct);

            return affected == 1;
        }

        public async Task ReleaseDeduplicationKeyAsync(int runId, CancellationToken ct = default)
        {
            await _dbSet
                .Where(j => j.Id == runId && j.DeduplicationKey != null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(j => j.DeduplicationKey, (string?)null),
                    ct);
        }
    }
}
