using Headbanger.Application.Pagination;
using Headbanger.Domain.Enums.Audit;

namespace Headbanger.Application.AuditLogs.Dtos
{
    public class AuditLogQueryDto : PagedRequestDto
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? EntityType { get; set; }
        public EAuditAction? Action { get; set; }
        public int? ActorUserId { get; set; }
        public string? EntityId { get; set; }
    }
}
