using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandVerificationRepository : IRepository<BandVerificationRequest>
    {
        Task<(List<BandVerificationRequest> Items, int TotalCount)> PageAsync(EBandVerificationStatus? status, string? search, int skip, int take);
        Task<Dictionary<EBandVerificationStatus, int>> CountByStatusAsync();
        Task<bool> TryReviewAsync(int requestId, EBandVerificationStatus status, int reviewerUserId, string? rejectionReason, DateTime reviewedAt);
    }
}
