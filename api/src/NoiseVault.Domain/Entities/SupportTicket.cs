using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Domain.Entities
{
    public class SupportTicket : EntityBase
    {
        public int ReporterUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ESupportTicketStatus Status { get; set; } = ESupportTicketStatus.Open;
        public ESupportTicketPriority Priority { get; set; } = ESupportTicketPriority.Normal;
        public string? OriginPath { get; set; }
        public string? UserAgent { get; set; }
        public int? ViewportWidth { get; set; }
        public int? ViewportHeight { get; set; }
        public string? InternalNote { get; set; }
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public User? Reporter { get; set; }
        public List<SupportTicketTag> Tags { get; set; } = [];
        public List<SupportTicketArea> Areas { get; set; } = [];
        public List<SupportTicketMedia> Media { get; set; } = [];
        public List<SupportTicketMessage> Messages { get; set; } = [];
    }
}
