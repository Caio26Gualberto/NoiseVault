using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.Notifications
{
    public static class NotificationRules
    {
        public const int RetentionDays = 90;

        private static readonly HashSet<ENotificationKind> Groupable =
        [
            ENotificationKind.PostLiked,
            ENotificationKind.PostCommented,
            ENotificationKind.EventCommented,
            ENotificationKind.EventInterest,
            ENotificationKind.ArchiveLiked,
            ENotificationKind.ArchiveCommented,
            ENotificationKind.ArchiveAttended,
            ENotificationKind.ArchiveCommentedWhereAttended,
            ENotificationKind.SupportTicketReplied,
        ];

        public static bool IsGroupable(ENotificationKind kind) => Groupable.Contains(kind);
    }
}
