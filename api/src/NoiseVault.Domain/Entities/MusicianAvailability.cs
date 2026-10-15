using NoiseVault.Domain.Enums.Ensaio;

namespace NoiseVault.Domain.Entities
{
    public class MusicianAvailability
    {
        public int MusicianProfileId { get; set; }
        public EWeekDay Day { get; set; }
        public EDayPeriod Period { get; set; }
    }
}
