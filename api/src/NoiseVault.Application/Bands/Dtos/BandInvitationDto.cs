using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandInvitationDto
    {
        public int Id { get; set; }
        public BandSummaryDto Band { get; set; } = new();
        public ProfileSummaryDto InvitedUser { get; set; } = new();
        public ProfileSummaryDto InvitedBy { get; set; } = new();
        public string? Instrument { get; set; }
        public EBandInvitationStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
