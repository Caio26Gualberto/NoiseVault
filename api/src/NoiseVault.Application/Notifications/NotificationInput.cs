using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.Notifications
{
    public sealed record NotificationInput(
        ENotificationKind Kind,
        int? ActorUserId,
        int? PostId = null,
        int? EventId = null,
        int? ArchiveEntryId = null,
        int? BandId = null,
        int? BandInvitationId = null);
}
