using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Support.Dtos;
using NoiseVault.Application.Support.Interfaces;
using NoiseVault.Domain.Constants;

namespace NoiseVault.Api.Controllers.Admin
{
    [Route("api/admin/support")]
    public class SupportAdminController : ApiBaseController
    {
        private readonly ISupportAppService _supportAppService;

        public SupportAdminController(ISupportAppService supportAppService)
        {
            _supportAppService = supportAppService;
        }

        [HttpGet("tickets")]
        [Authorize(Policy = Permissions.Support.View)]
        public async Task<ActionResult> List([FromQuery] SupportTicketListQueryDto query)
        {
            var result = await _supportAppService.List(query);
            return ToResponse(result);
        }

        [HttpGet("summary")]
        [Authorize(Policy = Permissions.Support.View)]
        public async Task<ActionResult> Summary()
        {
            var result = await _supportAppService.Summary();
            return ToResponse(result);
        }

        [HttpGet("tickets/{id:int}")]
        [Authorize(Policy = Permissions.Support.View)]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _supportAppService.Get(id);
            return ToResponse(result);
        }

        [HttpGet("tickets/{id:int}/messages")]
        [Authorize(Policy = Permissions.Support.View)]
        public async Task<ActionResult> Messages(int id, [FromQuery] PagedRequestDto query)
        {
            var result = await _supportAppService.Messages(id, query);
            return ToResponse(result);
        }

        [HttpPost("tickets/{id:int}/messages")]
        [Authorize(Policy = Permissions.Support.Reply)]
        [EnableRateLimiting(ApiRateLimitPolicies.CommentCreate)]
        public async Task<ActionResult> Reply(int id, [FromBody] SupportMessageInputDto input)
        {
            var result = await _supportAppService.ReplyAsStaff(id, input);
            return ToResponse(result);
        }

        [HttpPut("tickets/{id:int}")]
        [Authorize(Policy = Permissions.Support.Edit)]
        public async Task<ActionResult> Update(int id, [FromBody] SupportTicketUpdateInputDto input)
        {
            var result = await _supportAppService.Update(id, input);
            return ToResponse(result);
        }

        [HttpDelete("tickets/{id:int}")]
        [Authorize(Policy = Permissions.Support.Delete)]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _supportAppService.Delete(id);
            return ToResponse(result);
        }
    }
}
