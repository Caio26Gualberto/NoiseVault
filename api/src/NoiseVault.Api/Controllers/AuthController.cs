using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.Authentication;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Application.Auth.Dtos;
using NoiseVault.Application.Auth.Interfaces;
using NoiseVault.Application.Response;

namespace NoiseVault.Api.Controllers
{
    [Route("api/auth")]
    public class AuthController : ApiBaseController
    {
        private readonly IAuthAppService _authAppService;
        private readonly RefreshCookie _refreshCookie;
        private readonly OriginGuard _originGuard;

        public AuthController(IAuthAppService authAppService, RefreshCookie refreshCookie, OriginGuard originGuard)
        {
            _authAppService = authAppService;
            _refreshCookie = refreshCookie;
            _originGuard = originGuard;
        }

        [AllowAnonymous]
        [EnableRateLimiting(ApiRateLimitPolicies.Login)]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginInputDto input)
        {
            var result = await _authAppService.Login(input);

            if (result.IsSuccess && result.Data?.Tokens != null)
                _refreshCookie.Write(Response, result.Data.Tokens.RefreshToken);

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
        [EnableRateLimiting(ApiRateLimitPolicies.ConfirmEmail)]
        [HttpPost("confirm-email")]
        public async Task<ActionResult> ConfirmEmail([FromBody] ConfirmEmailInputDto input)
        {
            var result = await _authAppService.ConfirmEmail(input);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting(ApiRateLimitPolicies.ResendConfirmation)]
        [HttpPost("resend-confirmation")]
        public async Task<ActionResult> ResendConfirmation([FromBody] ResendConfirmationInputDto input)
        {
            var result = await _authAppService.ResendConfirmation(input);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting(ApiRateLimitPolicies.ForgotPassword)]
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordInputDto input)
        {
            var result = await _authAppService.ForgotPassword(input);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting(ApiRateLimitPolicies.ResetPassword)]
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordInputDto input)
        {
            var result = await _authAppService.ResetPassword(input);
            return ToResponse(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting(ApiRateLimitPolicies.RefreshToken)]
        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken()
        {
            if (!_originGuard.IsAllowed(Request))
                return ToResponse(Result.Fail(OriginNotAllowed()));

            var result = await _authAppService.RefreshTokens(new RefreshTokenRequestDto { RefreshToken = _refreshCookie.Read(Request) });

            if (result.IsSuccess)
                _refreshCookie.Write(Response, result.Data!.RefreshToken);

            return ToResponse(result);
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            if (!_originGuard.IsAllowed(Request))
                return ToResponse(Result.Fail(OriginNotAllowed()));

            var result = await _authAppService.Logout();
            _refreshCookie.Clear(Response);
            return ToResponse(result);
        }

        private static Error OriginNotAllowed() =>
            new("ORIGIN_NOT_ALLOWED", "This request did not come from an allowed site", ErrorType.Forbidden);
    }
}
