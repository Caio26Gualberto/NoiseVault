using NoiseVault.Application.Media.Dtos;

namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
        public int? Month { get; set; }
        public int? Day { get; set; }
        public MediaDto? Cover { get; set; }
        public int PhotoCount { get; set; }
        public int VideoCount { get; set; }
    }
}
