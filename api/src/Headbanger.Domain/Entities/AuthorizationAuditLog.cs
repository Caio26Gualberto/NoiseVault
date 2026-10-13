namespace Headbanger.Domain.Entities
{
    public class AuthorizationAuditLog : EntityBase
    {
        public string Action { get; set; } = string.Empty;
        public int ActorUserId { get; set; }
        public string ActorEmail { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }
        public string? IpAddress { get; set; }
    }
}
