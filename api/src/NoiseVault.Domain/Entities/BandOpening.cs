using NoiseVault.Domain.Enums.Ensaio;

namespace NoiseVault.Domain.Entities
{
    public class BandOpening : EntityBase
    {
        public int BandId { get; set; }
        public EInstrument Instrument { get; set; }
        public string? OtherName { get; set; }
        public string Description { get; set; } = string.Empty;
        public EMusicianGoal? Goal { get; set; }
        public EBandOpeningStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public Band? Band { get; set; }
        public List<BandOpeningInterest> Interests { get; set; } = [];
    }
}
