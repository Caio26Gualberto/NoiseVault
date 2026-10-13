using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Domain.Entities
{
    public class BandInvitation : EntityBase
    {
        public int BandId { get; set; }
        public int InvitedUserId { get; set; }
        public int InvitedByUserId { get; set; }
        public string? Instrument { get; set; }
        public EBandInvitationStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        public Band? Band { get; set; }
        public User? InvitedUser { get; set; }
        public User? InvitedByUser { get; set; }
    }
}
