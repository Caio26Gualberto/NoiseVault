using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public ENotificationKind Kind { get; set; }
        public int? ActorUserId { get; set; }
        public int ActorCount { get; set; } = 1;
        public int? PostId { get; set; }
        public int? EventId { get; set; }
        public int? ArchiveEntryId { get; set; }
        public int? BandId { get; set; }
        public int? BandInvitationId { get; set; }
        public int? SupportTicketId { get; set; }
        public int? BandOpeningId { get; set; }
        public int? BandPartnershipId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        public User? Actor { get; set; }
        public Post? Post { get; set; }
        public Event? Event { get; set; }
        public ArchiveEntry? ArchiveEntry { get; set; }
        public Band? Band { get; set; }
        public BandInvitation? BandInvitation { get; set; }
        public SupportTicket? SupportTicket { get; set; }
        public BandOpening? BandOpening { get; set; }
        public BandPartnership? BandPartnership { get; set; }
    }
}
