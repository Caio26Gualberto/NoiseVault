using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Api.Share;
using NoiseVault.Application.Share.Interfaces;

namespace NoiseVault.Api.Controllers.Share
{
    [ApiController]
    [AllowAnonymous]
    [Route("s")]
    public class ShareController : ControllerBase
    {
        private readonly ISharePreviewAppService _sharePreviewAppService;
        private readonly string _frontendBaseUrl;

        public ShareController(ISharePreviewAppService sharePreviewAppService, IConfiguration configuration)
        {
            _sharePreviewAppService = sharePreviewAppService;
            _frontendBaseUrl = (configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/');
        }

        [HttpGet("{section}/{key}")]
        [EnableRateLimiting(ApiRateLimitPolicies.SharePreview)]
        public async Task<ContentResult> Preview(string section, string key)
        {
            var preview = await _sharePreviewAppService.Build(section, key);
            var shareUrl = $"{_frontendBaseUrl}/s/{Uri.EscapeDataString(section)}/{Uri.EscapeDataString(key)}";

            Response.Headers.CacheControl = "public, max-age=300";

            return new ContentResult
            {
                Content = SharePage.Render(preview, shareUrl, _frontendBaseUrl),
                ContentType = "text/html; charset=utf-8",
                StatusCode = preview == null ? StatusCodes.Status404NotFound : StatusCodes.Status200OK,
            };
        }
    }
}
