using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Dtos;
using NoiseVault.Application.Posts.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/posts")]
    public class PostsController : ApiBaseController
    {
        private readonly IPostAppService _postAppService;
        private readonly ICommentAppService _commentAppService;

        public PostsController(IPostAppService postAppService, ICommentAppService commentAppService)
        {
            _postAppService = postAppService;
            _commentAppService = commentAppService;
        }

        [HttpGet]
        public async Task<ActionResult> Feed([FromQuery] FeedQueryDto query)
        {
            var result = await _postAppService.Feed(query);
            return ToResponse(result);
        }

        [HttpGet("/api/users/{nickname}/posts")]
        public async Task<ActionResult> ByUser(string nickname, [FromQuery] PagedRequestDto query)
        {
            var result = await _postAppService.ByUser(nickname, query);
            return ToResponse(result);
        }

        [HttpGet("/api/bands/{slug}/posts")]
        public async Task<ActionResult> ByBand(string slug, [FromQuery] PagedRequestDto query)
        {
            var result = await _postAppService.ByBand(slug, query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.PostCreate)]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreatePostInputDto input)
        {
            var result = await _postAppService.Create(input);
            return ToResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _postAppService.Get(id);
            return ToResponse(result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdatePostInputDto input)
        {
            var result = await _postAppService.Update(id, input);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _postAppService.Delete(id);
            return ToResponse(result);
        }

        [HttpPut("{id:int}/like")]
        public async Task<ActionResult> Like(int id)
        {
            var result = await _postAppService.Like(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}/like")]
        public async Task<ActionResult> Unlike(int id)
        {
            var result = await _postAppService.Unlike(id);
            return ToResponse(result);
        }

        [HttpGet("{id:int}/likes")]
        public async Task<ActionResult> Likes(int id, [FromQuery] PagedRequestDto query)
        {
            var result = await _postAppService.Likers(id, query);
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
    }
}
