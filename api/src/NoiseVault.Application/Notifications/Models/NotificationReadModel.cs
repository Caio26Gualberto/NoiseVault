using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Ensaio;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Profiles;
using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Notifications.Models
{
    public sealed class NotificationReadModel
    {
        public int Id { get; init; }
        public ENotificationKind Kind { get; init; }
        public int ActorCount { get; init; }
        public DateTime LastActivityAt { get; init; }
        public DateTime? ReadAt { get; init; }

        public int? ActorId { get; init; }
        public string? ActorName { get; init; }
        public string? ActorNickname { get; init; }
        public string? ActorAvatarKey { get; init; }

        public int? PostId { get; init; }
        public string? PostContent { get; init; }

        public int? EventId { get; init; }
        public string? EventTitle { get; init; }
        public DateTime? EventStartsAt { get; init; }

        public int? ArchiveEntryId { get; init; }
        public string? ArchiveTitle { get; init; }

        public int? BandId { get; init; }
        public string? BandName { get; init; }
        public string? BandSlug { get; init; }
        public string? BandAvatarKey { get; init; }
        public bool BandIsVerified { get; init; }
        public EBandKind? BandKind { get; init; }
        public List<EMusicGenre> BandGenres { get; init; } = [];

        public int? InvitationId { get; init; }
        public EBandInvitationStatus? InvitationStatus { get; init; }
        public string? InvitationInstrument { get; init; }
        public DateTime? InvitationExpiresAt { get; init; }

        public int? TicketId { get; init; }
        public string? TicketTitle { get; init; }
        public ESupportTicketStatus? TicketStatus { get; init; }
        public bool TicketMine { get; init; }

        public int? OpeningId { get; init; }
        public EInstrument? OpeningInstrument { get; init; }
        public string? OpeningOtherName { get; init; }
    }
}
