using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Domain.Entities
{
    public class ArchiveEntry : EntityBase
    {
        public int AuthorUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public DateTime SortDate { get; set; }
        public string? Venue { get; set; }
        public EAbcCity? City { get; set; }
        public string? CityName { get; set; }
        public int? CoverMediaUploadId { get; set; }

        public User? Author { get; set; }
        public List<ArchiveBandEntry> Bands { get; set; } = [];
        public List<ArchiveMedia> Media { get; set; } = [];
        public List<ArchiveLike> Likes { get; set; } = [];
        public List<ArchiveAttendance> Attendances { get; set; } = [];
        public List<ArchiveComment> Comments { get; set; } = [];
    }
}
