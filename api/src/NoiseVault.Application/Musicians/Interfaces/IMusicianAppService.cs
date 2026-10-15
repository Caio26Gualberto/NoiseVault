using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Geo;
using NoiseVault.Application.Musicians.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Musicians.Interfaces
{
    public interface IMusicianAppService
    {
        Task<Result<MusicianProfileDto>> GetMine();
        Task<Result<MusicianProfileDto>> SaveMine(MusicianInputDto input);
        Task<Result<MusicianProfileDto>> SetLooking(MusicianLookingInputDto input);
        Task<Result<MusicianProfileDto>> SetPhoto(ImageUploadInput? file, CancellationToken cancellationToken = default);
        Task<Result<MusicianProfileDto>> RemovePhoto();
        Task<Result<PagedResultDto<MusicianCardDto>>> Search(MusicianSearchQueryDto query);
        Task<Result<MusicianProfileDto>> GetByNickname(string nickname);

        Task<bool> IsLooking(int userId);
        Task<bool> HasProfile(int userId);
        Task<Dictionary<int, MusicianCardDto>> CardsByUserIds(IReadOnlyCollection<int> userIds);
        Task<GeoPoint?> ResolveOrigin(int? nearCityId);
    }
}
