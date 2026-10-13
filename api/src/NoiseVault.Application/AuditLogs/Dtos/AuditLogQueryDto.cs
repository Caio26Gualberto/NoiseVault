using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Audit;

namespace NoiseVault.Application.AuditLogs.Dtos
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
