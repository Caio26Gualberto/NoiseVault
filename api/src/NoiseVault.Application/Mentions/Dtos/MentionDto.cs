using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Mentions;

namespace NoiseVault.Application.Mentions.Dtos
{
    public class MentionDto
    {
        public string Handle { get; set; } = string.Empty;
        public EMentionKind Kind { get; set; }
        public EBandKind? BandKind { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
