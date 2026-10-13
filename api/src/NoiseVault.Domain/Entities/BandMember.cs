using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Domain.Entities
{
    public class BandMember : EntityBase
    {
        public int BandId { get; set; }
        public int UserId { get; set; }
        public string? Instrument { get; set; }
        public EBandMemberRole Role { get; set; }
        public DateTime JoinedAt { get; set; }

        public Band? Band { get; set; }
        public User? User { get; set; }
    }
}
