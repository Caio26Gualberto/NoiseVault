using NoiseVault.Domain.Enums.Media;

namespace NoiseVault.Application.PhotoTags.Models
{
    public sealed class PhotoTarget
    {
        public int MediaId { get; init; }
        public EMediaKind Kind { get; init; }
        public EMediaStatus Status { get; init; }
        public int? PostId { get; init; }
        public int? ArchiveEntryId { get; init; }
        public int? ContentAuthorUserId { get; init; }
    }
}
