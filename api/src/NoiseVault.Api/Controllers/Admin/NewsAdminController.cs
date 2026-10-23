using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoiseVault.Application.News.Dtos;
using NoiseVault.Application.News.Interfaces;
using NoiseVault.Domain.Constants;

namespace NoiseVault.Api.Controllers.Admin
{
    [Route("api/admin/news")]
    public class NewsAdminController : ApiBaseController
    {
        private const long ImportRequestLimit = 4 * 1024 * 1024;

        private readonly INewsAppService _newsAppService;

        public NewsAdminController(INewsAppService newsAppService)
        {
            _newsAppService = newsAppService;
        }

        [HttpPost("import/pending")]
        [Authorize(Policy = Permissions.News.Import)]
        [RequestSizeLimit(ImportRequestLimit)]
        public async Task<ActionResult> ImportPending([FromBody] NewsImportPendingInputDto input)
            => ToResponse(await _newsAppService.ImportPending(input));

        [HttpPost("import")]
        [Authorize(Policy = Permissions.News.Import)]
        [RequestSizeLimit(ImportRequestLimit)]
        public async Task<ActionResult> Import([FromBody] NewsImportInputDto input)
            => ToResponse(await _newsAppService.Import(input));
    }
}
