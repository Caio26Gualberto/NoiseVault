using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandAppService
    {
        Task<Result<BandProfileDto>> Create(CreateBandInputDto input);
        Task<Result<BandProfileDto>> GetBySlug(string slug);
        Task<Result<BandProfileDto>> Update(string slug, BandInputDto input);
        Task<Result<List<BandSummaryDto>>> Suggested();
        Task<Result<List<BandSummaryDto>>> Mine();
        Task<Result<BandProfileDto>> SetAvatar(string slug, ImageUploadInput? file, CancellationToken cancellationToken = default);
        Task<Result<BandProfileDto>> RemoveAvatar(string slug);
        Task<Result<BandProfileDto>> SetBanner(string slug, ImageUploadInput? file, int? position, CancellationToken cancellationToken = default);
        Task<Result<BandProfileDto>> SetBannerPosition(string slug, BannerPositionInputDto input);
        Task<Result<BandProfileDto>> RemoveBanner(string slug);
        Task<Result<BandProfileDto>> RemoveMember(string slug, int userId);
        Task<Result<BandProfileDto>> PromoteMember(string slug, int userId);
        Task<Result> Leave(string slug);

        Task<List<BandSummaryDto>> ListSummariesForUser(int userId);
        Task<Dictionary<int, BandSummaryDto>> SummariesByIds(IReadOnlyCollection<int> bandIds);
        Task<(int? BandId, EBandMemberRole? Role)> FindAccess(string slug);
        Task<bool> IsMember(int bandId, int userId);
        Task<int> CountMemberships(int userId);
        Task AddMember(int bandId, int userId, string? instrument);
    }
}
