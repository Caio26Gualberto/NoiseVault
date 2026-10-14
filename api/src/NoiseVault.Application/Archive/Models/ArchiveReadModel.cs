using NoiseVault.Application.Cities.Dtos;
using NoiseVault.Domain.Enums.Archive;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Archive.Models
{
    public sealed class ArchiveReadModel
    {
        public int Id { get; init; }
        public int AuthorUserId { get; init; }
        public EArchiveEntryStatus Status { get; init; }
        public bool IsSubmission { get; init; }
        public string? RejectionReason { get; init; }
        public DateTime? ReviewedAt { get; init; }
        public string AuthorName { get; init; } = string.Empty;
        public string AuthorNickname { get; init; } = string.Empty;
        public string? AuthorAvatarKey { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int Year { get; init; }
        public int? Month { get; init; }
        public int? Day { get; init; }
        public string? Venue { get; init; }
        public CityDto? City { get; init; }
        public string? CityName { get; init; }
        public int? CoverMediaUploadId { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<ArchiveBandRow> Bands { get; init; } = [];
        public int PhotoCount { get; init; }
        public int VideoCount { get; init; }
        public int LikeCount { get; init; }
        public bool LikedByMe { get; init; }
        public int AttendanceCount { get; init; }
        public bool AttendedByMe { get; init; }
        public int CommentCount { get; init; }
    }

    public sealed class ArchiveBandRow
    {
        public int? BandId { get; init; }
        public string? Name { get; init; }
    }
}
