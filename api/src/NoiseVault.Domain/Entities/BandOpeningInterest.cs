using NoiseVault.Domain.Enums.Ensaio;

namespace NoiseVault.Domain.Entities
{
    public class BandOpeningInterest : EntityBase
    {
        public int BandOpeningId { get; set; }
        public int UserId { get; set; }
        public string? Message { get; set; }
        public EOpeningInterestStatus Status { get; set; }
        public int? BandInvitationId { get; set; }

        public BandOpening? BandOpening { get; set; }
        public User? User { get; set; }
    }
}
