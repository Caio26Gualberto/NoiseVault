using Headbanger.Application.AuditLogs.Dtos;
using Headbanger.Application.Pagination;
using Headbanger.Application.Response;

namespace Headbanger.Application.AuditLogs.Interfaces
{
    public interface IAuditLogAppService
    {
        Task<Result<PagedResultDto<AuditLogListItemDto>>> GetAuditLogs(AuditLogQueryDto query);
        Task<Result<AuditLogFiltersDto>> GetFilters();
    }
}
