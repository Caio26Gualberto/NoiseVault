using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.Media.Interfaces;
using NoiseVault.Domain.Enums.Media;

namespace NoiseVault.Api.Controllers
{
    [Route("api/media")]
    public class MediaController : ApiBaseController
    {
        private const long PhotoRequestLimit = 16 * 1024 * 1024;

        private readonly IMediaUploadAppService _mediaAppService;

        public MediaController(IMediaUploadAppService mediaAppService)
        {
            _mediaAppService = mediaAppService;
        }

        [EnableRateLimiting(ApiRateLimitPolicies.MediaPhoto)]
        [RequestSizeLimit(PhotoRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = PhotoRequestLimit)]
        [HttpPost("photos")]
        public async Task<ActionResult> UploadPhoto(IFormFile? file, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _mediaAppService.UploadPhoto(input, EMediaProfile.Post, cancellationToken);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.MediaVideo)]
        [HttpPost("videos")]
        public async Task<ActionResult> StartVideo([FromBody] StartVideoUploadInputDto input)
        {
            var result = await _mediaAppService.StartVideo(input, EMediaProfile.Post);
            return ToResponse(result);
        }

        [HttpPost("videos/{id:int}/complete")]
        public async Task<ActionResult> CompleteVideo(int id, [FromBody] CompleteVideoUploadInputDto input)
        {
            var result = await _mediaAppService.CompleteVideo(id, input);
            return ToResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _mediaAppService.Get(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _mediaAppService.Delete(id);
            return ToResponse(result);
        }
    }
}
