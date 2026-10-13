using Headbanger.Application.AuditLogs.Dtos;
using Headbanger.Application.AuditLogs.Interfaces;
using Headbanger.Application.Common.Time;
using Headbanger.Application.Pagination;
using Headbanger.Application.Response;

namespace Headbanger.Application.AuditLogs
{
    public class AuditLogAppService : IAuditLogAppService
    {
        private readonly IAuditLogRepository _repository;

        public AuditLogAppService(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<PagedResultDto<AuditLogListItemDto>>> GetAuditLogs(AuditLogQueryDto query)
        {
            query.Normalize();

            query.EntityType = Normalize(query.EntityType);
            query.EntityId = Normalize(query.EntityId);

            var (items, totalCount) = await _repository.ListAsync(
                query,
                AppTime.DayStartUtc(query.From),
                AppTime.NextDayStartUtc(query.To));

            var paged = PagedResultDto<AuditLogListItemDto>.Create(items, query.Page, query.PageSize, totalCount);

            return Result<PagedResultDto<AuditLogListItemDto>>.Ok(
                paged,
                "AUDIT_LOGS_LOADED",
                "Audit logs loaded successfully");
        }

        public async Task<Result<AuditLogFiltersDto>> GetFilters()
        {
            var filters = await _repository.GetFiltersAsync();

            return Result<AuditLogFiltersDto>.Ok(
                filters,
                "AUDIT_LOG_FILTERS_LOADED",
                "Audit log filters loaded successfully");
        }

        private static string? Normalize(string? value)
        {
            var trimmed = value?.Trim();

            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}
