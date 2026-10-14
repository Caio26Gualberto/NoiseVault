using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;

namespace NoiseVault.Api.Controllers
{
    [Route("api/archive")]
    public class ArchiveController : ApiBaseController
    {
        private readonly IArchiveAppService _archiveAppService;
        private readonly IArchiveCommentAppService _commentAppService;

        public ArchiveController(IArchiveAppService archiveAppService, IArchiveCommentAppService commentAppService)
        {
            _archiveAppService = archiveAppService;
            _commentAppService = commentAppService;
        }

        [HttpGet]
        public async Task<ActionResult> List([FromQuery] ArchiveListQueryDto query)
        {
            var result = await _archiveAppService.List(query);
            return ToResponse(result);
        }

        [HttpGet("years")]
        public async Task<ActionResult> Years()
        {
            var result = await _archiveAppService.Years();
            return ToResponse(result);
        }

        [HttpGet("/api/bands/{slug}/archive")]
        public async Task<ActionResult> ByBand(string slug, [FromQuery] ArchiveListQueryDto query)
        {
            var result = await _archiveAppService.ByBand(slug, query);
            return ToResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _archiveAppService.Get(id);
            return ToResponse(result);
        }

        [HttpGet("{id:int}/attendees")]
        public async Task<ActionResult> Attendees(int id, [FromQuery] PagedRequestDto query)
        {
            var result = await _archiveAppService.Attendees(id, query);
            return ToResponse(result);
        }

        [HttpPut("{id:int}/like")]
        public async Task<ActionResult> Like(int id)
        {
            var result = await _archiveAppService.Like(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}/like")]
        public async Task<ActionResult> Unlike(int id)
        {
            var result = await _archiveAppService.Unlike(id);
            return ToResponse(result);
        }

        [HttpPut("{id:int}/attendance")]
        public async Task<ActionResult> Attend(int id)
        {
            var result = await _archiveAppService.Attend(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}/attendance")]
        public async Task<ActionResult> Unattend(int id)
        {
            var result = await _archiveAppService.Unattend(id);
            return ToResponse(result);
        }

        [HttpGet("{id:int}/comments")]
        public async Task<ActionResult> Comments(int id, [FromQuery] PagedRequestDto query)
        {
            var result = await _commentAppService.List(id, query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.CommentCreate)]
        [HttpPost("{id:int}/comments")]
        public async Task<ActionResult> AddComment(int id, [FromBody] CommentInputDto input)
        {
            var result = await _commentAppService.Add(id, input);
            return ToResponse(result);
        }

        [HttpDelete("/api/archive-comments/{id:int}")]
        public async Task<ActionResult> DeleteComment(int id)
        {
            var result = await _commentAppService.Delete(id);
            return ToResponse(result);
        }
    }
}
