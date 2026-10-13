using NoiseVault.Application.AuditLogs.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.AuditLogs.Interfaces
{
    public interface IAuditLogAppService
    {
        Task<Result<PagedResultDto<AuditLogListItemDto>>> GetAuditLogs(AuditLogQueryDto query);
        Task<Result<AuditLogFiltersDto>> GetFilters();
    }
}
