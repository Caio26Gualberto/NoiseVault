using Microsoft.EntityFrameworkCore;
using Headbanger.Application.BackgroundJobs.Interfaces;
using Headbanger.Domain.Entities;
using Headbanger.Domain.Enums.Jobs;
using Headbanger.Infra.Data.Context;

namespace Headbanger.Infra.Data.Repositories.BackgroundJobRuns
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
