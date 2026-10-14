using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Support.Dtos;
using NoiseVault.Application.Support.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/support")]
    public class SupportController : ApiBaseController
    {
        private const long PhotoRequestLimit = 16 * 1024 * 1024;

        private readonly ISupportAppService _supportAppService;

        public SupportController(ISupportAppService supportAppService)
        {
            _supportAppService = supportAppService;
        }

        [HttpPost("tickets")]
        [EnableRateLimiting(ApiRateLimitPolicies.SupportCreate)]
        public async Task<ActionResult> Create([FromBody] SupportTicketInputDto input)
        {
            var result = await _supportAppService.Create(input);
            return ToResponse(result);
        }

        [HttpGet("tickets")]
        public async Task<ActionResult> List([FromQuery] PagedRequestDto query)
        {
            var result = await _supportAppService.ListMine(query);
            return ToResponse(result);
        }

        [HttpGet("tickets/{id:int}")]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _supportAppService.GetMine(id);
            return ToResponse(result);
        }

        [HttpGet("tickets/{id:int}/messages")]
        public async Task<ActionResult> Messages(int id, [FromQuery] PagedRequestDto query)
        {
            var result = await _supportAppService.MyMessages(id, query);
            return ToResponse(result);
        }

        [HttpPost("tickets/{id:int}/messages")]
        [EnableRateLimiting(ApiRateLimitPolicies.CommentCreate)]
        public async Task<ActionResult> Reply(int id, [FromBody] SupportMessageInputDto input)
        {
            var result = await _supportAppService.ReplyAsReporter(id, input);
            return ToResponse(result);
        }

        [HttpPost("media/photos")]
        [EnableRateLimiting(ApiRateLimitPolicies.MediaPhoto)]
        [RequestSizeLimit(PhotoRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = PhotoRequestLimit)]
        public async Task<ActionResult> UploadPhoto(IFormFile? file, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _supportAppService.UploadPhoto(input, cancellationToken);
            return ToResponse(result);
        }

        [HttpPost("media/videos")]
        [EnableRateLimiting(ApiRateLimitPolicies.MediaVideo)]
        public async Task<ActionResult> StartVideo([FromBody] StartVideoUploadInputDto input)
        {
            var result = await _supportAppService.StartVideo(input);
            return ToResponse(result);
        }
    }
}
