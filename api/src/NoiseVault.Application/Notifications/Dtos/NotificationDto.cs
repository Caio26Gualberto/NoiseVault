using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Ensaio;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Notifications.Dtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public ENotificationKind Kind { get; set; }
        public ProfileSummaryDto? Actor { get; set; }
        public int ActorCount { get; set; }
        public NotificationPostDto? Post { get; set; }
        public NotificationEventDto? Event { get; set; }
        public NotificationArchiveDto? Archive { get; set; }
        public BandSummaryDto? Band { get; set; }
        public NotificationInvitationDto? Invitation { get; set; }
        public NotificationTicketDto? Ticket { get; set; }
        public NotificationOpeningDto? Opening { get; set; }
        public NotificationPartnershipDto? Partnership { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime LastActivityAt { get; set; }
    }

    public class NotificationPostDto
    {
        public int Id { get; set; }
        public string Excerpt { get; set; } = string.Empty;
    }

    public class NotificationEventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
    }

    public class NotificationArchiveDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class NotificationTicketDto
    {
        public int Id { get; set; }
        public string Protocol { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ESupportTicketStatus Status { get; set; }
        public bool Mine { get; set; }
    }

    public class NotificationOpeningDto
    {
        public int Id { get; set; }
        public EInstrument Instrument { get; set; }
        public string? OtherName { get; set; }
    }

    public class NotificationPartnershipDto
    {
        public int Id { get; set; }
        public EBandPartnershipStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public BandSummaryDto MyBand { get; set; } = new();
    }

    public class NotificationInvitationDto
    {
        public int Id { get; set; }
        public EBandInvitationStatus Status { get; set; }
        public string? Instrument { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
