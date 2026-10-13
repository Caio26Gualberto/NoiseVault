using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoiseVault.Application.BackgroundJobs.Dtos;
using NoiseVault.Application.BackgroundJobs.Interfaces;
using NoiseVault.Domain.Constants;

namespace NoiseVault.Api.Controllers.Admin
{
    [Route("api/admin/jobs")]
    public class JobsController : ApiBaseController
    {
        private readonly IBackgroundJobAppService _backgroundJobAppService;

        public JobsController(IBackgroundJobAppService backgroundJobAppService)
        {
            _backgroundJobAppService = backgroundJobAppService;
        }

        [HttpGet("recurring")]
        [Authorize(Policy = Permissions.System.JobsView)]
        public async Task<ActionResult> Recurring()
        {
            var result = await _backgroundJobAppService.GetRecurring();
            return ToResponse(result);
        }

        [HttpGet("runs")]
        [Authorize(Policy = Permissions.System.JobsView)]
        public async Task<ActionResult> Runs([FromQuery] BackgroundJobQueryDto query)
        {
            var result = await _backgroundJobAppService.GetRuns(query);
            return ToResponse(result);
        }

        [HttpGet("runs/{id:int}")]
        [Authorize(Policy = Permissions.System.JobsView)]
        public async Task<ActionResult> Run(int id)
        {
            var result = await _backgroundJobAppService.GetRun(id);
            return ToResponse(result);
        }

        [HttpPost("runs/{id:int}/retry")]
        [Authorize(Policy = Permissions.System.JobsManage)]
        public async Task<ActionResult> Retry(int id)
        {
            var result = await _backgroundJobAppService.Retry(id);
            return ToResponse(result);
        }
    }
}
