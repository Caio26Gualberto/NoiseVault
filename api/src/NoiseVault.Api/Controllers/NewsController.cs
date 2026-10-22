using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoiseVault.Application.News.Dtos;
using NoiseVault.Application.News.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/news")]
    public class NewsController : ApiBaseController
    {
        private readonly INewsAppService _newsAppService;

        public NewsController(INewsAppService newsAppService)
        {
            _newsAppService = newsAppService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> List([FromQuery] NewsListQueryDto query)
            => ToResponse(await _newsAppService.List(query));
    }
}
