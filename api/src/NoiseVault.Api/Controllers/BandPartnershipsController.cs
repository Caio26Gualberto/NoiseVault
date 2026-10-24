using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Pagination;

namespace NoiseVault.Api.Controllers
{
    [Route("api")]
    public class BandPartnershipsController : ApiBaseController
    {
        private readonly IBandPartnershipAppService _partnershipAppService;

        public BandPartnershipsController(IBandPartnershipAppService partnershipAppService)
        {
            _partnershipAppService = partnershipAppService;
        }

        [AllowAnonymous]
        [HttpGet("bands/{slug}/partnerships")]
        public async Task<ActionResult> Accepted(string slug, [FromQuery] PagedRequestDto query)
        {
            var result = await _partnershipAppService.ListAccepted(slug, query);
            return ToResponse(result);
        }

        [HttpGet("bands/{slug}/partnerships/pending")]
        public async Task<ActionResult> Pending(string slug, [FromQuery] PagedRequestDto query)
        {
            var result = await _partnershipAppService.ListPending(slug, query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.BandPartnership)]
        [HttpPost("bands/{slug}/partnerships")]
        public async Task<ActionResult> Invite(string slug, [FromBody] InvitePartnerInputDto input)
        {
            var result = await _partnershipAppService.Invite(slug, input);
            return ToResponse(result);
        }

        [HttpPost("band-partnerships/{id:int}/accept")]
        public async Task<ActionResult> Accept(int id)
        {
            var result = await _partnershipAppService.Accept(id);
            return ToResponse(result);
        }

        [HttpPost("band-partnerships/{id:int}/decline")]
        public async Task<ActionResult> Decline(int id)
        {
            var result = await _partnershipAppService.Decline(id);
            return ToResponse(result);
        }

        [HttpDelete("band-partnerships/{id:int}")]
        public async Task<ActionResult> Remove(int id)
        {
            var result = await _partnershipAppService.Remove(id);
            return ToResponse(result);
        }
    }
}
