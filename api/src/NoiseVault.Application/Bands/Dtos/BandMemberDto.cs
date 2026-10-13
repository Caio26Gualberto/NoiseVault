using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandMemberDto
    {
        public ProfileSummaryDto User { get; set; } = new();
        public string? Instrument { get; set; }
        public EBandMemberRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
