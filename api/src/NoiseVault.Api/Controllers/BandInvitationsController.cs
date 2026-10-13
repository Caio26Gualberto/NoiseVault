using Microsoft.AspNetCore.Mvc;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Pagination;

namespace NoiseVault.Api.Controllers
{
    [Route("api/band-invitations")]
    public class BandInvitationsController : ApiBaseController
    {
        private readonly IBandInvitationAppService _invitationAppService;

        public BandInvitationsController(IBandInvitationAppService invitationAppService)
        {
            _invitationAppService = invitationAppService;
        }

        [HttpGet("mine")]
        public async Task<ActionResult> Mine([FromQuery] PagedRequestDto query)
        {
            var result = await _invitationAppService.ListMine(query);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/accept")]
        public async Task<ActionResult> Accept(int id)
        {
            var result = await _invitationAppService.Accept(id);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/decline")]
        public async Task<ActionResult> Decline(int id)
        {
            var result = await _invitationAppService.Decline(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Revoke(int id)
        {
            var result = await _invitationAppService.Revoke(id);
            return ToResponse(result);
        }
    }
}
