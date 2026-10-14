using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support.Dtos
{
    public class SupportTicketDto
    {
        public int Id { get; set; }
        public string Protocol { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ESupportTicketStatus Status { get; set; }
        public ESupportTicketPriority Priority { get; set; }
        public List<ESupportTag> Tags { get; set; } = [];
        public List<ESupportArea> Areas { get; set; } = [];
        public int PhotoCount { get; set; }
        public int VideoCount { get; set; }
        public List<MediaDto>? Media { get; set; }
        public int MessageCount { get; set; }
        public bool AwaitingStaff { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class SupportTicketAdminDto : SupportTicketDto
    {
        public ProfileSummaryDto Reporter { get; set; } = new();
        public string? OriginPath { get; set; }
        public string? UserAgent { get; set; }
        public int? ViewportWidth { get; set; }
        public int? ViewportHeight { get; set; }
        public string? InternalNote { get; set; }
        public bool CanEdit { get; set; }
        public bool CanReply { get; set; }
    }
}
