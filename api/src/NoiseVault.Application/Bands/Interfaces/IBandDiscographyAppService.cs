using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Bands.Interfaces
{
    public interface IBandDiscographyAppService
    {
        Task<Result<BandDiscographyDto>> Get(string slug);
        Task<Result<BandDiscographyDto>> Import(string slug);
        Task<Result<BandDiscographyDto>> CreateRelease(string slug, BandReleaseInputDto input);
        Task<Result<BandDiscographyDto>> UpdateRelease(string slug, int releaseId, BandReleaseInputDto input);
        Task<Result<BandDiscographyDto>> DeleteRelease(string slug, int releaseId);
        Task<Result<BandDiscographyDto>> SetCover(string slug, int releaseId, ImageUploadInput? file, CancellationToken cancellationToken = default);
        Task<Result<BandDiscographyDto>> Reorder(string slug, DiscographyOrderInputDto input);
        Task<Result<BandDiscographyDto>> SetHidden(string slug, int releaseId, ReleaseVisibilityInputDto input);
        Task<Result<BandDiscographyDto>> SetFeatured(string slug, FeaturedTrackInputDto input);
        Task<int> RefreshStale();
    }
}
