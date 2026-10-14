namespace NoiseVault.Application.Mentions
{
    public sealed record MentionTarget(bool InComment, int? PostId = null, int? EventId = null, int? ArchiveEntryId = null);
}
