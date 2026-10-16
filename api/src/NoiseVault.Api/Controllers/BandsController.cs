using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Profiles.Dtos;

namespace NoiseVault.Api.Controllers
{
    [Route("api/bands")]
    public class BandsController : ApiBaseController
    {
        private const long AvatarRequestLimit = 6 * 1024 * 1024;
        private const long BannerRequestLimit = 11 * 1024 * 1024;

        private readonly IBandAppService _bandAppService;
        private readonly IBandInvitationAppService _invitationAppService;
        private readonly IBandVerificationAppService _verificationAppService;
        private readonly IBandDiscographyAppService _discographyAppService;

        public BandsController(IBandAppService bandAppService, IBandInvitationAppService invitationAppService, IBandVerificationAppService verificationAppService, IBandDiscographyAppService discographyAppService)
        {
            _bandAppService = bandAppService;
            _invitationAppService = invitationAppService;
            _verificationAppService = verificationAppService;
            _discographyAppService = discographyAppService;
        }

        [EnableRateLimiting(ApiRateLimitPolicies.BandCreate)]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateBandInputDto input)
        {
            var result = await _bandAppService.Create(input);
            return ToResponse(result);
        }

        [HttpGet("suggested")]
        public async Task<ActionResult> Suggested()
        {
            var result = await _bandAppService.Suggested();
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.UserSearch)]
        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] BandSearchQueryDto query)
        {
            var result = await _bandAppService.Search(query);
            return ToResponse(result);
        }

        [HttpGet("mine")]
        public async Task<ActionResult> Mine()
        {
            var result = await _bandAppService.Mine();
            return ToResponse(result);
        }

        [HttpGet("following")]
        public async Task<ActionResult> Following([FromQuery] PagedRequestDto query)
        {
            var result = await _bandAppService.Following(query);
            return ToResponse(result);
        }

        [HttpGet("owned")]
        public async Task<ActionResult> Owned()
        {
            var result = await _bandAppService.Owned();
            return ToResponse(result);
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult> GetBySlug(string slug)
        {
            var result = await _bandAppService.GetBySlug(slug);
            return ToResponse(result);
        }

        [HttpPut("{slug}")]
        public async Task<ActionResult> Update(string slug, [FromBody] BandInputDto input)
        {
            var result = await _bandAppService.Update(slug, input);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.ProfileUpload)]
        [RequestSizeLimit(AvatarRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = AvatarRequestLimit)]
        [HttpPost("{slug}/avatar")]
        public async Task<ActionResult> SetAvatar(string slug, IFormFile? file, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _bandAppService.SetAvatar(slug, input, cancellationToken);
            return ToResponse(result);
        }

        [HttpDelete("{slug}/avatar")]
        public async Task<ActionResult> RemoveAvatar(string slug)
        {
            var result = await _bandAppService.RemoveAvatar(slug);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.ProfileUpload)]
        [RequestSizeLimit(BannerRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = BannerRequestLimit)]
        [HttpPost("{slug}/banner")]
        public async Task<ActionResult> SetBanner(string slug, IFormFile? file, [FromForm] int? position, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _bandAppService.SetBanner(slug, input, position, cancellationToken);
            return ToResponse(result);
        }

        [HttpPatch("{slug}/banner-position")]
        public async Task<ActionResult> SetBannerPosition(string slug, [FromBody] BannerPositionInputDto input)
        {
            var result = await _bandAppService.SetBannerPosition(slug, input);
            return ToResponse(result);
        }

        [HttpDelete("{slug}/banner")]
        public async Task<ActionResult> RemoveBanner(string slug)
        {
            var result = await _bandAppService.RemoveBanner(slug);
            return ToResponse(result);
        }

        [HttpGet("{slug}/invitations")]
        public async Task<ActionResult> ListInvitations(string slug, [FromQuery] PagedRequestDto query)
        {
            var result = await _invitationAppService.ListForBand(slug, query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.BandInvite)]
        [HttpPost("{slug}/invitations")]
        public async Task<ActionResult> Invite(string slug, [FromBody] InviteMemberInputDto input)
        {
            var result = await _invitationAppService.Invite(slug, input);
            return ToResponse(result);
        }

        [HttpDelete("{slug}/members/{userId:int}")]
        public async Task<ActionResult> RemoveMember(string slug, int userId)
        {
            var result = await _bandAppService.RemoveMember(slug, userId);
            return ToResponse(result);
        }

        [HttpPost("{slug}/members/{userId:int}/promote")]
        public async Task<ActionResult> PromoteMember(string slug, int userId)
        {
            var result = await _bandAppService.PromoteMember(slug, userId);
            return ToResponse(result);
        }

        [HttpPut("{slug}/follow")]
        public async Task<ActionResult> Follow(string slug)
        {
            var result = await _bandAppService.Follow(slug);
            return ToResponse(result);
        }

        [HttpDelete("{slug}/follow")]
        public async Task<ActionResult> Unfollow(string slug)
        {
            var result = await _bandAppService.Unfollow(slug);
            return ToResponse(result);
        }

        [HttpPost("{slug}/leave")]
        public async Task<ActionResult> Leave(string slug)
        {
            var result = await _bandAppService.Leave(slug);
            return ToResponse(result);
        }

        [HttpGet("{slug}/discography")]
        public async Task<ActionResult> Discography(string slug)
        {
            var result = await _discographyAppService.Get(slug);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.DiscographyImport)]
        [HttpPost("{slug}/discography/import")]
        public async Task<ActionResult> ImportDiscography(string slug)
        {
            var result = await _discographyAppService.Import(slug);
            return ToResponse(result);
        }

        [HttpPost("{slug}/discography/releases")]
        public async Task<ActionResult> CreateRelease(string slug, [FromBody] BandReleaseInputDto input)
        {
            var result = await _discographyAppService.CreateRelease(slug, input);
            return ToResponse(result);
        }

        [HttpPut("{slug}/discography/releases/{releaseId:int}")]
        public async Task<ActionResult> UpdateRelease(string slug, int releaseId, [FromBody] BandReleaseInputDto input)
        {
            var result = await _discographyAppService.UpdateRelease(slug, releaseId, input);
            return ToResponse(result);
        }

        [HttpDelete("{slug}/discography/releases/{releaseId:int}")]
        public async Task<ActionResult> DeleteRelease(string slug, int releaseId)
        {
            var result = await _discographyAppService.DeleteRelease(slug, releaseId);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.ProfileUpload)]
        [RequestSizeLimit(AvatarRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = AvatarRequestLimit)]
        [HttpPost("{slug}/discography/releases/{releaseId:int}/cover")]
        public async Task<ActionResult> SetReleaseCover(string slug, int releaseId, IFormFile? file, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _discographyAppService.SetCover(slug, releaseId, input, cancellationToken);
            return ToResponse(result);
        }

        [HttpPut("{slug}/discography/order")]
        public async Task<ActionResult> ReorderDiscography(string slug, [FromBody] DiscographyOrderInputDto input)
        {
            var result = await _discographyAppService.Reorder(slug, input);
            return ToResponse(result);
        }

        [HttpPatch("{slug}/discography/releases/{releaseId:int}/visibility")]
        public async Task<ActionResult> SetReleaseVisibility(string slug, int releaseId, [FromBody] ReleaseVisibilityInputDto input)
        {
            var result = await _discographyAppService.SetHidden(slug, releaseId, input);
            return ToResponse(result);
        }

        [HttpPut("{slug}/discography/featured")]
        public async Task<ActionResult> SetFeaturedTrack(string slug, [FromBody] FeaturedTrackInputDto input)
        {
            var result = await _discographyAppService.SetFeatured(slug, input);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.BandVerification)]
        [HttpPost("{slug}/verification")]
        public async Task<ActionResult> RequestVerification(string slug, [FromBody] BandVerificationRequestInputDto input)
        {
            var result = await _verificationAppService.Request(slug, input);
            return ToResponse(result);
        }
    }
}
