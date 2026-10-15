using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.BandOpenings.Dtos;
using NoiseVault.Application.BandOpenings.Interfaces;
using NoiseVault.Application.Pagination;

namespace NoiseVault.Api.Controllers
{
    [Route("api/openings")]
    public class BandOpeningsController : ApiBaseController
    {
        private readonly IBandOpeningAppService _openingAppService;

        public BandOpeningsController(IBandOpeningAppService openingAppService)
        {
            _openingAppService = openingAppService;
        }

        [HttpGet]
        public async Task<ActionResult> Search([FromQuery] BandOpeningSearchQueryDto query)
        {
            var result = await _openingAppService.Search(query);
            return ToResponse(result);
        }

        [HttpGet("/api/bands/{slug}/openings")]
        public async Task<ActionResult> ListForBand(string slug, [FromQuery] PagedRequestDto query)
        {
            var result = await _openingAppService.ListForBand(slug, query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.OpeningCreate)]
        [HttpPost("/api/bands/{slug}/openings")]
        public async Task<ActionResult> Create(string slug, [FromBody] BandOpeningInputDto input)
        {
            var result = await _openingAppService.Create(slug, input);
            return ToResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _openingAppService.Get(id);
            return ToResponse(result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] BandOpeningInputDto input)
        {
            var result = await _openingAppService.Update(id, input);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/close")]
        public async Task<ActionResult> Close(int id, [FromBody] CloseOpeningInputDto input)
        {
            var result = await _openingAppService.Close(id, input);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/renew")]
        public async Task<ActionResult> Renew(int id)
        {
            var result = await _openingAppService.Renew(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _openingAppService.Delete(id);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.OpeningInterest)]
        [HttpPost("{id:int}/interest")]
        public async Task<ActionResult> ExpressInterest(int id, [FromBody] OpeningInterestInputDto input)
        {
            var result = await _openingAppService.ExpressInterest(id, input);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}/interest")]
        public async Task<ActionResult> WithdrawInterest(int id)
        {
            var result = await _openingAppService.WithdrawInterest(id);
            return ToResponse(result);
        }

        [HttpGet("{id:int}/interests")]
        public async Task<ActionResult> ListInterests(int id, [FromQuery] PagedRequestDto query)
        {
            var result = await _openingAppService.ListInterests(id, query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.BandInvite)]
        [HttpPost("{id:int}/interests/{interestId:int}/invite")]
        public async Task<ActionResult> InviteInterested(int id, int interestId, [FromBody] InviteFromInterestInputDto input)
        {
            var result = await _openingAppService.InviteInterested(id, interestId, input);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/interests/{interestId:int}/dismiss")]
        public async Task<ActionResult> DismissInterested(int id, int interestId)
        {
            var result = await _openingAppService.DismissInterested(id, interestId);
            return ToResponse(result);
        }
    }
}
