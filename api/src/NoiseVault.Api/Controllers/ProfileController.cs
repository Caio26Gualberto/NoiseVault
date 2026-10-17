using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.Authentication;
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
        private readonly RefreshCookie _refreshCookie;

        public ProfileController(IProfileAppService profileAppService, IAuthAppService authAppService, RefreshCookie refreshCookie)
        {
            _profileAppService = profileAppService;
            _authAppService = authAppService;
            _refreshCookie = refreshCookie;
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

        [EnableRateLimiting(ApiRateLimitPolicies.LocationUpdate)]
        [HttpPut("location")]
        public async Task<ActionResult> SetApproxLocation([FromBody] ApproxLocationInputDto input)
        {
            var result = await _profileAppService.SetApproxLocation(input);
            return ToResponse(result);
        }

        [HttpDelete("location")]
        public async Task<ActionResult> ClearApproxLocation()
        {
            var result = await _profileAppService.ClearApproxLocation();
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

            if (result.IsSuccess)
                _refreshCookie.Write(Response, result.Data!.RefreshToken);

            return ToResponse(result);
        }

        [HttpPost("set-initial-password")]
        public async Task<ActionResult> SetInitialPassword([FromBody] SetInitialPasswordInputDto input)
        {
            var result = await _authAppService.SetInitialPassword(input);

            if (result.IsSuccess)
                _refreshCookie.Write(Response, result.Data!.RefreshToken);

            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.SignInMethod)]
        [HttpPost("create-password")]
        public async Task<ActionResult> CreatePassword([FromBody] CreatePasswordInputDto input)
        {
            var result = await _authAppService.CreatePassword(input);
            return ToResponse(result);
        }

        [HttpGet("sign-in-methods")]
        public async Task<ActionResult> SignInMethods()
        {
            var result = await _authAppService.GetSignInMethods();
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.SignInMethod)]
        [HttpPost("sign-in-methods")]
        public async Task<ActionResult> LinkSignInMethod([FromBody] ExternalLoginInputDto input)
        {
            var result = await _authAppService.LinkSignInMethod(input);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.SignInMethod)]
        [HttpDelete("sign-in-methods/{provider:int}")]
        public async Task<ActionResult> UnlinkSignInMethod(int provider)
        {
            var result = await _authAppService.UnlinkSignInMethod(provider);
            return ToResponse(result);
        }
    }
}
