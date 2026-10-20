using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.RateLimiting;
using NoiseVault.Api.Seo;
using NoiseVault.Application.Seo.Interfaces;

namespace NoiseVault.Api.Controllers.Seo
{
    [ApiController]
    [AllowAnonymous]
    [Route("seo")]
    [EnableRateLimiting(ApiRateLimitPolicies.SeoPage)]
    public class SeoController : ControllerBase
    {
        private const string CacheControl = "public, max-age=600";

        private readonly ISeoPageAppService _seoPageAppService;
        private readonly string _frontendBaseUrl;

        public SeoController(ISeoPageAppService seoPageAppService, IConfiguration configuration)
        {
            _seoPageAppService = seoPageAppService;
            _frontendBaseUrl = (configuration["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/');
        }

        [HttpGet("")]
        public Task<ContentResult> Home() => Page("home", null);

        [HttpGet("{section}")]
        public Task<ContentResult> List(string section) => Page(section, null);

        [HttpGet("{section}/{key}")]
        public Task<ContentResult> Detail(string section, string key) => Page(section, key);

        [HttpGet("sitemap.xml")]
        public async Task<ContentResult> Sitemap()
        {
            var entries = await _seoPageAppService.Sitemap();

            Response.Headers.CacheControl = CacheControl;

            return new ContentResult
            {
                Content = SeoPage.Sitemap(entries, _frontendBaseUrl),
                ContentType = "application/xml; charset=utf-8",
                StatusCode = StatusCodes.Status200OK,
            };
        }

        private async Task<ContentResult> Page(string section, string? key)
        {
            var page = await _seoPageAppService.Build(section, key);

            Response.Headers.CacheControl = CacheControl;

            return new ContentResult
            {
                Content = page == null ? SeoPage.NotFound(_frontendBaseUrl) : SeoPage.Render(page, _frontendBaseUrl),
                ContentType = "text/html; charset=utf-8",
                StatusCode = page == null ? StatusCodes.Status404NotFound : StatusCodes.Status200OK,
            };
        }
    }
}
