using NoiseVault.Application.Bands.Dtos;

namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveBandDto
    {
        public BandSummaryDto? Band { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
