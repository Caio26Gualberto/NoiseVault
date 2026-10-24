using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Domain.Entities
{
    public class BandPartnership : EntityBase
    {
        public int RequesterBandId { get; set; }
        public int TargetBandId { get; set; }
        public int LowBandId { get; set; }
        public int HighBandId { get; set; }
        public int InvitedByUserId { get; set; }
        public int? RespondedByUserId { get; set; }
        public EBandPartnershipStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        public Band? RequesterBand { get; set; }
        public Band? TargetBand { get; set; }
        public User? InvitedByUser { get; set; }
    }
}
