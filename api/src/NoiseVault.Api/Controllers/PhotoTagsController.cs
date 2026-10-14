using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.PhotoTags.Dtos;
using NoiseVault.Application.PhotoTags.Interfaces;

namespace NoiseVault.Api.Controllers
{
    public class PhotoTagsController : ApiBaseController
    {
        private readonly IPhotoTagAppService _photoTagAppService;

        public PhotoTagsController(IPhotoTagAppService photoTagAppService)
        {
            _photoTagAppService = photoTagAppService;
        }

        [HttpPost("/api/media/{mediaId:int}/tags")]
        [EnableRateLimiting(ApiRateLimitPolicies.PhotoTag)]
        public async Task<ActionResult> Add(int mediaId, [FromBody] PhotoTagInputDto input)
        {
            var result = await _photoTagAppService.Add(mediaId, input);
            return ToResponse(result);
        }

        [HttpDelete("/api/photo-tags/{id:int}")]
        public async Task<ActionResult> Remove(int id)
        {
            var result = await _photoTagAppService.Remove(id);
            return ToResponse(result);
        }
    }
}
