using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Domain.Entities
{
    public class EventGenre
    {
        public int EventId { get; set; }
        public EMusicGenre Genre { get; set; }
    }
}
