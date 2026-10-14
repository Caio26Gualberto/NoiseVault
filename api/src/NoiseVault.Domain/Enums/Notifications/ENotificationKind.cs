namespace NoiseVault.Domain.Enums.Notifications
{
    public enum ENotificationKind
    {
        PostLiked = 0,
        PostCommented = 1,
        EventCommented = 2,
        EventInterest = 3,
        EventCancelled = 4,
        EventTomorrow = 5,
        EventLineup = 6,
        ArchiveLiked = 7,
        ArchiveCommented = 8,
        ArchiveAttended = 9,
        ArchiveCommentedWhereAttended = 10,
        BandInvite = 11,
        BandInviteAccepted = 12,
        BandInviteDeclined = 13,
        BandPromoted = 14,
        BandRemoved = 15,
        BandMemberLeft = 16,
        SupportTicketCreated = 17,
        SupportTicketReplied = 18,
        SupportTicketStatusChanged = 19,
        ArchiveSubmissionReceived = 20,
        ArchiveSubmissionApproved = 21,
        ArchiveSubmissionRejected = 22,
    }
}
