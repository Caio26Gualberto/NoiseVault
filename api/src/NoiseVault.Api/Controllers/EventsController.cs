using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Events.Dtos;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.Profiles.Dtos;

namespace NoiseVault.Api.Controllers
{
    [Route("api/events")]
    public class EventsController : ApiBaseController
    {
        private const long BannerRequestLimit = 11 * 1024 * 1024;

        private readonly IEventAppService _eventAppService;
        private readonly IEventCommentAppService _commentAppService;

        public EventsController(IEventAppService eventAppService, IEventCommentAppService commentAppService)
        {
            _eventAppService = eventAppService;
            _commentAppService = commentAppService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> List([FromQuery] EventListQueryDto query)
        {
            var result = await _eventAppService.List(query);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("upcoming")]
        public async Task<ActionResult> Upcoming([FromQuery] int take = 3)
        {
            var result = await _eventAppService.Upcoming(take);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("/api/bands/{slug}/events")]
        public async Task<ActionResult> ByBand(string slug, [FromQuery] EventListQueryDto query)
        {
            var result = await _eventAppService.ByBand(slug, query);
            return ToResponse(result);
        }

        [HttpGet("/api/users/{nickname}/events")]
        public async Task<ActionResult> ByUser(string nickname, [FromQuery] EventListQueryDto query)
        {
            var result = await _eventAppService.ByUser(nickname, query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.EventCreate)]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateEventInputDto input)
        {
            var result = await _eventAppService.Create(input);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _eventAppService.Get(id);
            return ToResponse(result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] EventInputDto input)
        {
            var result = await _eventAppService.Update(id, input);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult> Cancel(int id)
        {
            var result = await _eventAppService.Cancel(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _eventAppService.Delete(id);
            return ToResponse(result);
        }

        [HttpPut("{id:int}/interest")]
        public async Task<ActionResult> AddInterest(int id)
        {
            var result = await _eventAppService.AddInterest(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}/interest")]
        public async Task<ActionResult> RemoveInterest(int id)
        {
            var result = await _eventAppService.RemoveInterest(id);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.ProfileUpload)]
        [RequestSizeLimit(BannerRequestLimit)]
        [RequestFormLimits(MultipartBodyLengthLimit = BannerRequestLimit)]
        [HttpPost("{id:int}/banner")]
        public async Task<ActionResult> SetBanner(int id, IFormFile? file, [FromForm] int? position, CancellationToken cancellationToken)
        {
            await using var stream = file?.OpenReadStream();
            var input = stream is null ? null : new ImageUploadInput(stream, file!.Length);
            var result = await _eventAppService.SetBanner(id, input, position, cancellationToken);
            return ToResponse(result);
        }

        [HttpPatch("{id:int}/banner-position")]
        public async Task<ActionResult> SetBannerPosition(int id, [FromBody] BannerPositionInputDto input)
        {
            var result = await _eventAppService.SetBannerPosition(id, input);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}/banner")]
        public async Task<ActionResult> RemoveBanner(int id)
        {
            var result = await _eventAppService.RemoveBanner(id);
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

        [HttpDelete("/api/event-comments/{id:int}")]
        public async Task<ActionResult> DeleteComment(int id)
        {
            var result = await _commentAppService.Delete(id);
            return ToResponse(result);
        }
    }
}
