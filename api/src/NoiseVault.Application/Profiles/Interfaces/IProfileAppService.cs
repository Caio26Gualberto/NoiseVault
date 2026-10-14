using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Profiles.Interfaces
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
        Task<Result<ProfileDto>> UpdateNotificationSettings(NotificationSettingsInputDto input);
    }
}
