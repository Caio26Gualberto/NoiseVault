using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandPartnershipRepository : IRepository<BandPartnership>
    {
        Task<bool> TryRespondAsync(int partnershipId, int ownerUserId, EBandPartnershipStatus status, DateTime now);
        Task<bool> TryEndAsync(int partnershipId, int ownerUserId, DateTime now);
        Task RetireExpiredAsync(int lowBandId, int highBandId, DateTime now);
    }
}
