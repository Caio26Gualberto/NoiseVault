using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Musicians.Dtos;
using NoiseVault.Application.Musicians.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/musicians")]
    public class MusiciansController : ApiBaseController
    {
        private const long PhotoRequestLimit = 11 * 1024 * 1024;

        private readonly IMusicianAppService _musicianAppService;

        public MusiciansController(IMusicianAppService musicianAppService)
        {
            _musicianAppService = musicianAppService;
        }

        [EnableRateLimiting(ApiRateLimitPolicies.UserSearch)]
        [HttpGet]
        public async Task<ActionResult> Search([FromQuery] MusicianSearchQueryDto query)
        {
            var result = await _musicianAppService.Search(query);
            return ToResponse(result);
        }

        [HttpGet("me")]
        public async Task<ActionResult> GetMine()
        {
            var result = await _musicianAppService.GetMine();
            return ToResponse(result);
        }

        [HttpPut("me")]
        public async Task<ActionResult> SaveMine([FromBody] MusicianInputDto input)
        {
            var result = await _musicianAppService.SaveMine(input);
            return ToResponse(result);
        }

        [HttpPost("me/looking")]
        public async Task<ActionResult> SetLooking([FromBody] MusicianLookingInputDto input)
        {
            var result = await _musicianAppService.SetLooking(input);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.ProfileUpload)]
        [RequestSizeLimit(PhotoRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = PhotoRequestLimit)]
        [HttpPost("me/photo")]
        public async Task<ActionResult> SetPhoto(IFormFile? file, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _musicianAppService.SetPhoto(input, cancellationToken);
            return ToResponse(result);
        }

        [HttpDelete("me/photo")]
        public async Task<ActionResult> RemovePhoto()
        {
            var result = await _musicianAppService.RemovePhoto();
            return ToResponse(result);
        }

        [HttpGet("{nickname}")]
        public async Task<ActionResult> GetByNickname(string nickname)
        {
            var result = await _musicianAppService.GetByNickname(nickname);
            return ToResponse(result);
        }
    }
}
