using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Domain.Entities
{
    public class BandVerificationRequest : EntityBase
    {
        public int BandId { get; set; }
        public int RequestedByUserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public EBandVerificationStatus Status { get; set; }
        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
        public Band? Band { get; set; }
        public User? RequestedBy { get; set; }
        public User? ReviewedBy { get; set; }
    }
}
