using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveEntryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public string? Venue { get; set; }
        public EAbcCity? City { get; set; }
        public string? CityName { get; set; }
        public ProfileSummaryDto Author { get; set; } = new();
        public List<ArchiveBandDto> Bands { get; set; } = [];
        public MediaDto? Cover { get; set; }
        public List<ArchiveMediaDto> Media { get; set; } = [];
        public int PhotoCount { get; set; }
        public int VideoCount { get; set; }
        public int LikeCount { get; set; }
        public bool LikedByMe { get; set; }
        public int AttendanceCount { get; set; }
        public bool AttendedByMe { get; set; }
        public List<ProfileSummaryDto> Attendees { get; set; } = [];
        public int CommentCount { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
