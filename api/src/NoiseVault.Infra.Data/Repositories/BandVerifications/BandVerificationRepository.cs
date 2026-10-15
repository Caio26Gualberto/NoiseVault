using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.BandVerifications
{
    public class BandVerificationRepository : Repository<BandVerificationRequest>, IBandVerificationRepository
    {
        public BandVerificationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<(List<BandVerificationRequest> Items, int TotalCount)> PageAsync(EBandVerificationStatus? status, string? search, int skip, int take)
        {
            var query = _dbSet.Where(r => !r.IsDeleted);

            if (status != null)
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(r => r.Band!.Name.ToLower().Contains(term)
                    || r.Band.Slug.Contains(term)
                    || r.RequestedBy!.Name.ToLower().Contains(term)
                    || r.RequestedBy.Nickname.Contains(term));
            }

            var totalCount = await query.CountAsync();

            var ordered = status == EBandVerificationStatus.Pending
                ? query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
                : query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);

            var items = await ordered
                .Skip(skip)
                .Take(take)
                .Include(r => r.Band!).ThenInclude(b => b.Genres)
                .Include(r => r.RequestedBy)
                .Include(r => r.ReviewedBy)
                .AsNoTracking()
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Dictionary<EBandVerificationStatus, int>> CountByStatusAsync() =>
            await _dbSet
                .Where(r => !r.IsDeleted)
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

        public async Task<bool> TryReviewAsync(int requestId, EBandVerificationStatus status, int reviewerUserId, string? rejectionReason, DateTime reviewedAt) =>
            await _dbSet
                .Where(r => r.Id == requestId && !r.IsDeleted && r.Status == EBandVerificationStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, status)
                    .SetProperty(r => r.ReviewedByUserId, reviewerUserId)
                    .SetProperty(r => r.ReviewedAt, reviewedAt)
                    .SetProperty(r => r.RejectionReason, rejectionReason)
                    .SetProperty(r => r.UpdatedAt, reviewedAt)
                    .SetProperty(r => r.UpdatedBy, reviewerUserId)) == 1;
    }
}
