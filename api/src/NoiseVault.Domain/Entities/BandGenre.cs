using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Domain.Entities
{
    public class BandGenre
    {
        public int BandId { get; set; }
        public EMusicGenre Genre { get; set; }
    }
}
