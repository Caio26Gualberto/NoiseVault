using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Auth.Dtos;
using NoiseVault.Application.Auth.Interfaces;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.Profiles.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/profile")]
    public class ProfileController : ApiBaseController
    {
        private const long AvatarRequestLimit = 6 * 1024 * 1024;
        private const long BannerRequestLimit = 11 * 1024 * 1024;

        private readonly IProfileAppService _profileAppService;
        private readonly IAuthAppService _authAppService;

        public ProfileController(IProfileAppService profileAppService, IAuthAppService authAppService)
        {
            _profileAppService = profileAppService;
            _authAppService = authAppService;
        }

        [HttpGet]
        public async Task<ActionResult> Me()
        {
            var result = await _profileAppService.Me();
            return ToResponse(result);
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] UpdateProfileInputDto input)
        {
            var result = await _profileAppService.Update(input);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.ProfileUpload)]
        [RequestSizeLimit(AvatarRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = AvatarRequestLimit)]
        [HttpPost("avatar")]
        public async Task<ActionResult> SetAvatar(IFormFile? file, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _profileAppService.SetAvatar(input, cancellationToken);
            return ToResponse(result);
        }

        [HttpDelete("avatar")]
        public async Task<ActionResult> RemoveAvatar()
        {
            var result = await _profileAppService.RemoveAvatar();
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.ProfileUpload)]
        [RequestSizeLimit(BannerRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = BannerRequestLimit)]
        [HttpPost("banner")]
        public async Task<ActionResult> SetBanner(IFormFile? file, [FromForm] int? position, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _profileAppService.SetBanner(input, position, cancellationToken);
            return ToResponse(result);
        }

        [HttpPut("notification-settings")]
        public async Task<ActionResult> UpdateNotificationSettings([FromBody] NotificationSettingsInputDto input)
        {
            var result = await _profileAppService.UpdateNotificationSettings(input);
            return ToResponse(result);
        }

        [HttpPatch("banner-position")]
        public async Task<ActionResult> SetBannerPosition([FromBody] BannerPositionInputDto input)
        {
            var result = await _profileAppService.SetBannerPosition(input);
            return ToResponse(result);
        }

        [HttpDelete("banner")]
        public async Task<ActionResult> RemoveBanner()
        {
            var result = await _profileAppService.RemoveBanner();
            return ToResponse(result);
        }

        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordInputDto input)
        {
            var result = await _authAppService.ChangePassword(input);
            return ToResponse(result);
        }

        [HttpPost("set-initial-password")]
        public async Task<ActionResult> SetInitialPassword([FromBody] SetInitialPasswordInputDto input)
        {
            var result = await _authAppService.SetInitialPassword(input);
            return ToResponse(result);
        }
    }
}
