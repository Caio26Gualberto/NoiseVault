using Microsoft.AspNetCore.Mvc;
using Headbanger.Application.Auth.Dtos;
using Headbanger.Application.Auth.Interfaces;
using Headbanger.Application.Profiles.Dtos;
using Headbanger.Application.Profiles.Interfaces;

namespace Headbanger.Api.Controllers
{
    [Route("api/profile")]
    public class ProfileController : ApiBaseController
    {
        private readonly IProfileAppService _profileAppService;
        private readonly IAuthAppService _authAppService;

        public ProfileController(IProfileAppService profileAppService, IAuthAppService authAppService)
        {
            _profileAppService = profileAppService;
            _authAppService = authAppService;
        }

        [HttpGet]
        public async Task<ActionResult> Me()
        {
            var result = await _profileAppService.Me();
            return ToResponse(result);
        }

        [HttpPatch("name")]
        public async Task<ActionResult> UpdateName([FromBody] UpdateProfileNameDto input)
        {
            var result = await _profileAppService.UpdateName(input);
            return ToResponse(result);
        }

        [HttpPost("complete-onboarding")]
        public async Task<ActionResult> CompleteOnboarding()
        {
            var result = await _profileAppService.CompleteOnboarding();
            return ToResponse(result);
        }

        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordInputDto input)
        {
            var result = await _authAppService.ChangePassword(input);
            return ToResponse(result);
        }

        [HttpPost("set-initial-password")]
        public async Task<ActionResult> SetInitialPassword([FromBody] SetInitialPasswordInputDto input)
        {
            var result = await _authAppService.SetInitialPassword(input);
            return ToResponse(result);
        }
    }
}
