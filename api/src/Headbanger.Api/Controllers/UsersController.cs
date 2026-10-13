using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Headbanger.Api.RateLimiting;
using Headbanger.Application.PublicProfiles.Dtos;
using Headbanger.Application.PublicProfiles.Interfaces;

namespace Headbanger.Api.Controllers
{
    [Route("api/users")]
    public class UsersController : ApiBaseController
    {
        private readonly IPublicProfileAppService _publicProfileAppService;

        public UsersController(IPublicProfileAppService publicProfileAppService)
        {
            _publicProfileAppService = publicProfileAppService;
        }

        [EnableRateLimiting(ApiRateLimitPolicies.UserSearch)]
        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] UserSearchQueryDto query)
        {
            var result = await _publicProfileAppService.Search(query);
            return ToResponse(result);
        }

        [HttpGet("{nickname}")]
        public async Task<ActionResult> GetByNickname(string nickname)
        {
            var result = await _publicProfileAppService.GetByNickname(nickname);
            return ToResponse(result);
        }
    }
}
