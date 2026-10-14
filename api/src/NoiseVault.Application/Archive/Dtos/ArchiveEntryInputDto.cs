using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveEntryInputDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public string? Venue { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public List<ArchiveBandInputDto>? Bands { get; set; }
        public List<ArchiveMediaInputDto>? Media { get; set; }
        public int? CoverMediaId { get; set; }
    }
}
