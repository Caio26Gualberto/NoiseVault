using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public EBandKind Kind { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
        public List<EMusicGenre> Genres { get; set; } = [];
    }
}
