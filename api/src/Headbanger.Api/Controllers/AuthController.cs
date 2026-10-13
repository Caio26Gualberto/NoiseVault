using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Headbanger.Api.RateLimiting;
using Headbanger.Application.Auth.Dtos;
using Headbanger.Application.Auth.Interfaces;

namespace Headbanger.Api.Controllers
{
    [Route("api/auth")]
    public class AuthController : ApiBaseController
    {
        private readonly IAuthAppService _authAppService;

        public AuthController(IAuthAppService authAppService)
        {
            _authAppService = authAppService;
        }

        [AllowAnonymous]
        [EnableRateLimiting(ApiRateLimitPolicies.Login)]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginInputDto input)
        {
            var result = await _authAppService.Login(input);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting(ApiRateLimitPolicies.Register)]
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterInputDto input)
        {
            var result = await _authAppService.Register(input);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting(ApiRateLimitPolicies.RefreshToken)]
        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequestDto input)
        {
            var result = await _authAppService.RefreshTokens(input);
            return ToResponse(result);
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            var result = await _authAppService.Logout();
            return ToResponse(result);
        }
    }
}
