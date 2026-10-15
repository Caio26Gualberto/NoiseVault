using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Domain.Entities
{
    public class MusicianGenre
    {
        public int MusicianProfileId { get; set; }
        public EMusicGenre Genre { get; set; }
    }
}
