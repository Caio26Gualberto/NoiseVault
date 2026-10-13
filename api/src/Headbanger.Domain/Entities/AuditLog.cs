using Headbanger.Domain.Enums.Audit;

namespace Headbanger.Domain.Entities
{
    public class AuditLog
    {
        public long Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public int ActorUserId { get; set; }
        public string ActorEmail { get; set; } = string.Empty;
        public string ActorName { get; set; } = string.Empty;
        public EAuditAction Action { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? EntityDescription { get; set; }
        public string? ChangesJson { get; set; }
        public string? IpAddress { get; set; }
    }
}
