using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support.Models
{
    public sealed class SupportTicketReadModel
    {
        public int Id { get; init; }
        public int ReporterUserId { get; init; }
        public string ReporterName { get; init; } = string.Empty;
        public string ReporterNickname { get; init; } = string.Empty;
        public string? ReporterAvatarKey { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public ESupportTicketStatus Status { get; init; }
        public ESupportTicketPriority Priority { get; init; }
        public List<ESupportTag> Tags { get; init; } = [];
        public List<ESupportArea> Areas { get; init; } = [];
        public int PhotoCount { get; init; }
        public int VideoCount { get; init; }
        public int MessageCount { get; init; }
        public bool? LastMessageFromStaff { get; init; }
        public string? OriginPath { get; init; }
        public string? UserAgent { get; init; }
        public int? ViewportWidth { get; init; }
        public int? ViewportHeight { get; init; }
        public string? InternalNote { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastActivityAt { get; init; }
        public DateTime? ClosedAt { get; init; }
    }
}
