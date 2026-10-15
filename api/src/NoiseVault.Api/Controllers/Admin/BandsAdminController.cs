using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Domain.Constants;

namespace NoiseVault.Api.Controllers.Admin
{
    [Route("api/admin/bands")]
    public class BandsAdminController : ApiBaseController
    {
        private readonly IBandVerificationAppService _verificationAppService;

        public BandsAdminController(IBandVerificationAppService verificationAppService)
        {
            _verificationAppService = verificationAppService;
        }

        [HttpGet("verifications")]
        [Authorize(Policy = Permissions.Bands.VerificationsView)]
        public async Task<ActionResult> Verifications([FromQuery] BandVerificationQueryDto query)
        {
            var result = await _verificationAppService.List(query);
            return ToResponse(result);
        }

        [HttpGet("verifications/summary")]
        [Authorize(Policy = Permissions.Bands.VerificationsView)]
        public async Task<ActionResult> VerificationSummary()
        {
            var result = await _verificationAppService.Summary();
            return ToResponse(result);
        }

        [HttpPost("verifications/{id:int}/approve")]
        [Authorize(Policy = Permissions.Bands.Verify)]
        public async Task<ActionResult> Approve(int id)
        {
            var result = await _verificationAppService.Approve(id);
            return ToResponse(result);
        }

        [HttpPost("verifications/{id:int}/reject")]
        [Authorize(Policy = Permissions.Bands.Verify)]
        public async Task<ActionResult> Reject(int id, [FromBody] BandVerificationRejectInputDto input)
        {
            var result = await _verificationAppService.Reject(id, input);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/unverify")]
        [Authorize(Policy = Permissions.Bands.Verify)]
        public async Task<ActionResult> Unverify(int id)
        {
            var result = await _verificationAppService.Unverify(id);
            return ToResponse(result);
        }
    }
}
