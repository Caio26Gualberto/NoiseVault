using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandInvitationRepository : IRepository<BandInvitation>
    {
        Task<bool> TryRespondAsync(int invitationId, int invitedUserId, EBandInvitationStatus status, DateTime now);
        Task<bool> TryRevokeAsync(int invitationId, int bandId, DateTime now);
        Task RetireExpiredAsync(int bandId, int invitedUserId, DateTime now);
    }
}
