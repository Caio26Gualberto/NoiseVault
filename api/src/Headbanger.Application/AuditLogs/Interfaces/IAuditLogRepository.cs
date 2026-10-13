using Headbanger.Application.AuditLogs.Dtos;

namespace Headbanger.Application.AuditLogs.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<(IReadOnlyList<AuditLogListItemDto> Items, int TotalCount)> ListAsync(
            AuditLogQueryDto query,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken ct = default);

        Task<AuditLogFiltersDto> GetFiltersAsync(CancellationToken ct = default);
    }
}
