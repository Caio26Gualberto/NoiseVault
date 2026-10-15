using NoiseVault.Domain.Enums.Ensaio;

namespace NoiseVault.Domain.Entities
{
    public class MusicianInstrument
    {
        public int Id { get; set; }
        public int MusicianProfileId { get; set; }
        public EInstrument Instrument { get; set; }
        public string? OtherName { get; set; }
        public ESkillLevel Level { get; set; }
        public int Position { get; set; }
    }
}
