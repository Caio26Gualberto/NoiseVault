using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.BandPartnerships
{
    public class BandPartnershipRepository : Repository<BandPartnership>, IBandPartnershipRepository
    {
        public BandPartnershipRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> TryRespondAsync(int partnershipId, int ownerUserId, EBandPartnershipStatus status, DateTime now)
        {
            var affected = await _dbSet
                .Where(p => p.Id == partnershipId
                    && p.Status == EBandPartnershipStatus.Pending
                    && p.ExpiresAt > now
                    && !p.IsDeleted
                    && p.TargetBand!.Members.Any(m => m.UserId == ownerUserId && m.Role == EBandMemberRole.Owner))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, status)
                    .SetProperty(p => p.RespondedAt, now)
                    .SetProperty(p => p.RespondedByUserId, ownerUserId)
                    .SetProperty(p => p.UpdatedAt, now)
                    .SetProperty(p => p.UpdatedBy, ownerUserId));

            return affected == 1;
        }

        public async Task<bool> TryEndAsync(int partnershipId, int ownerUserId, DateTime now)
        {
            var affected = await _dbSet
                .Where(p => p.Id == partnershipId
                    && !p.IsDeleted
                    && ((p.Status == EBandPartnershipStatus.Pending
                            && p.RequesterBand!.Members.Any(m => m.UserId == ownerUserId && m.Role == EBandMemberRole.Owner))
                        || (p.Status == EBandPartnershipStatus.Accepted
                            && (p.RequesterBand!.Members.Any(m => m.UserId == ownerUserId && m.Role == EBandMemberRole.Owner)
                                || p.TargetBand!.Members.Any(m => m.UserId == ownerUserId && m.Role == EBandMemberRole.Owner)))))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, EBandPartnershipStatus.Revoked)
                    .SetProperty(p => p.UpdatedAt, now)
                    .SetProperty(p => p.UpdatedBy, ownerUserId));

            return affected == 1;
        }

        public async Task RetireExpiredAsync(int lowBandId, int highBandId, DateTime now)
        {
            await _dbSet
                .Where(p => p.LowBandId == lowBandId
                    && p.HighBandId == highBandId
                    && p.Status == EBandPartnershipStatus.Pending
                    && p.ExpiresAt <= now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, EBandPartnershipStatus.Revoked)
                    .SetProperty(p => p.UpdatedAt, now));
        }
    }
}
