using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Headbanger.Application.AuditLogs.Dtos;
using Headbanger.Application.AuditLogs.Interfaces;
using Headbanger.Domain.Constants;

namespace Headbanger.Api.Controllers.Admin
{
    [Route("api/admin/audit")]
    public class AuditController : ApiBaseController
    {
        private readonly IAuditLogAppService _auditLogAppService;

        public AuditController(IAuditLogAppService auditLogAppService)
        {
            _auditLogAppService = auditLogAppService;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.System.AuditView)]
        public async Task<ActionResult> List([FromQuery] AuditLogQueryDto query)
        {
            var result = await _auditLogAppService.GetAuditLogs(query);
            return ToResponse(result);
        }

        [HttpGet("filters")]
        [Authorize(Policy = Permissions.System.AuditView)]
        public async Task<ActionResult> Filters()
        {
            var result = await _auditLogAppService.GetFilters();
            return ToResponse(result);
        }
    }
}
