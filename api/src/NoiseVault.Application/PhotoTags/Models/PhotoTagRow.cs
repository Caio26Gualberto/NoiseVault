namespace NoiseVault.Application.PhotoTags.Models
{
    public sealed class PhotoTagRow
    {
        public int Id { get; init; }
        public int MediaUploadId { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
        public int? TaggedUserId { get; init; }
        public string? TaggedUserName { get; init; }
        public string? TaggedUserNickname { get; init; }
        public string? TaggedUserAvatarKey { get; init; }
        public string? Name { get; init; }
        public int TaggedByUserId { get; init; }
        public int? ContentAuthorUserId { get; init; }
        public bool InArchive { get; init; }
        public int? PostId { get; init; }
        public int? ArchiveEntryId { get; init; }
    }
}
