using NoiseVault.Domain.Enums.Ensaio;

namespace NoiseVault.Domain.Entities
{
    public class MusicianProfile : EntityBase
    {
        public int UserId { get; set; }
        public bool IsLookingForBand { get; set; }
        public DateTime? LookingSince { get; set; }
        public EMusicianGoal Goal { get; set; }
        public EMusicianCommitment Commitment { get; set; }
        public EReach Reach { get; set; }
        public bool HasGear { get; set; }
        public bool HasTransport { get; set; }
        public bool HasRehearsalSpace { get; set; }
        public string? Influences { get; set; }

        public User? User { get; set; }
        public List<MusicianInstrument> Instruments { get; set; } = [];
        public List<MusicianGenre> Genres { get; set; } = [];
        public List<MusicianAvailability> Availability { get; set; } = [];
        public List<MusicianSampleLink> SampleLinks { get; set; } = [];
    }
}
