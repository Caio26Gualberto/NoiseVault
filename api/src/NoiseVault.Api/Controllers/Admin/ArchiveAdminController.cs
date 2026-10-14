using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Domain.Constants;

namespace NoiseVault.Api.Controllers.Admin
{
    [Route("api/admin/archive")]
    public class ArchiveAdminController : ApiBaseController
    {
        private const long PhotoRequestLimit = 41 * 1024 * 1024;

        private readonly IArchiveAppService _archiveAppService;

        public ArchiveAdminController(IArchiveAppService archiveAppService)
        {
            _archiveAppService = archiveAppService;
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Archive.Create)]
        public async Task<ActionResult> Create([FromBody] ArchiveEntryInputDto input)
        {
            var result = await _archiveAppService.Create(input);
            return ToResponse(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = Permissions.Archive.Edit)]
        public async Task<ActionResult> Update(int id, [FromBody] ArchiveEntryInputDto input)
        {
            var result = await _archiveAppService.Update(id, input);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = Permissions.Archive.Delete)]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _archiveAppService.Delete(id);
            return ToResponse(result);
        }

        [HttpPost("media/photos")]
        [Authorize(Policy = Permissions.Archive.Create)]
        [EnableRateLimiting(ApiRateLimitPolicies.MediaPhoto)]
        [RequestSizeLimit(PhotoRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = PhotoRequestLimit)]
        public async Task<ActionResult> UploadPhoto(IFormFile? file, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _archiveAppService.UploadPhoto(input, cancellationToken);
            return ToResponse(result);
        }

        [HttpPost("media/videos")]
        [Authorize(Policy = Permissions.Archive.Create)]
        [EnableRateLimiting(ApiRateLimitPolicies.MediaVideo)]
        public async Task<ActionResult> StartVideo([FromBody] StartVideoUploadInputDto input)
        {
            var result = await _archiveAppService.StartVideo(input);
            return ToResponse(result);
        }
    }
}
