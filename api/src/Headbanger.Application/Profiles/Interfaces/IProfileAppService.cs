using Headbanger.Application.Profiles.Dtos;
using Headbanger.Application.Response;

namespace Headbanger.Application.Profiles.Interfaces
{
    public interface IProfileAppService
    {
        Task<Result<ProfileDto>> Me();
        Task<Result<ProfileDto>> Update(UpdateProfileInputDto input);
        Task<Result<ProfileDto>> SetAvatar(ImageUploadInput? file, CancellationToken cancellationToken = default);
        Task<Result<ProfileDto>> RemoveAvatar();
        Task<Result<ProfileDto>> SetBanner(ImageUploadInput? file, int? position, CancellationToken cancellationToken = default);
        Task<Result<ProfileDto>> SetBannerPosition(BannerPositionInputDto input);
        Task<Result<ProfileDto>> RemoveBanner();
    }
}
