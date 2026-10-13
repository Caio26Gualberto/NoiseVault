using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.BandInvitations
{
    public class BandInvitationRepository : Repository<BandInvitation>, IBandInvitationRepository
    {
        public BandInvitationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> TryRespondAsync(int invitationId, int invitedUserId, EBandInvitationStatus status, DateTime now)
        {
            var affected = await _dbSet
                .Where(i => i.Id == invitationId
                    && i.InvitedUserId == invitedUserId
                    && i.Status == EBandInvitationStatus.Pending
                    && i.ExpiresAt > now
                    && !i.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Status, status)
                    .SetProperty(i => i.RespondedAt, now)
                    .SetProperty(i => i.UpdatedAt, now)
                    .SetProperty(i => i.UpdatedBy, invitedUserId));

            return affected == 1;
        }

        public async Task<bool> TryRevokeAsync(int invitationId, int bandId, DateTime now)
        {
            var affected = await _dbSet
                .Where(i => i.Id == invitationId
                    && i.BandId == bandId
                    && i.Status == EBandInvitationStatus.Pending
                    && !i.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Status, EBandInvitationStatus.Revoked)
                    .SetProperty(i => i.RespondedAt, now)
                    .SetProperty(i => i.UpdatedAt, now));

            return affected == 1;
        }

        public async Task RetireExpiredAsync(int bandId, int invitedUserId, DateTime now)
        {
            await _dbSet
                .Where(i => i.BandId == bandId
                    && i.InvitedUserId == invitedUserId
                    && i.Status == EBandInvitationStatus.Pending
                    && i.ExpiresAt <= now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.Status, EBandInvitationStatus.Revoked)
                    .SetProperty(i => i.UpdatedAt, now));
        }
    }
}
