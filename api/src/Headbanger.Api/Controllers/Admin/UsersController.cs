using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Headbanger.Application.Users.Dtos;
using Headbanger.Application.Users.Interfaces;
using Headbanger.Domain.Constants;

namespace Headbanger.Api.Controllers.Admin
{
    [Route("api/admin/users")]
    public class UsersController : ApiBaseController
    {
        private readonly IUserAppService _userAppService;

        public UsersController(IUserAppService userAppService)
        {
            _userAppService = userAppService;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Authorization.UsersView)]
        public async Task<ActionResult> List([FromQuery] UserListQueryDto query)
        {
            var result = await _userAppService.GetAll(query);
            return ToResponse(result);
        }

        [HttpGet("{userId:int}")]
        [Authorize(Policy = Permissions.Authorization.UsersView)]
        public async Task<ActionResult> Get(int userId)
        {
            var result = await _userAppService.Get(userId);
            return ToResponse(result);
        }

        [HttpDelete("{userId:int}")]
        [Authorize(Policy = Permissions.Authorization.UsersDelete)]
        public async Task<ActionResult> RevokeAccess(int userId)
        {
            var result = await _userAppService.RevokeAccess(userId);
            return ToResponse(result);
        }

        [HttpPost("invite")]
        [Authorize(Policy = Permissions.Authorization.UsersInvite)]
        public async Task<ActionResult> Invite([FromBody] InviteUserInputDto input)
        {
            var result = await _userAppService.Invite(input);
            return ToResponse(result);
        }
    }
}
