using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandPartnershipDto
    {
        public int Id { get; set; }
        public BandSummaryDto Partner { get; set; } = new();
        public EBandPartnershipDirection Direction { get; set; }
        public EBandPartnershipStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
