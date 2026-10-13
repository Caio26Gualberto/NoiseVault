using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Domain.Entities
{
    public class UserGenre
    {
        public int UserId { get; set; }
        public EMusicGenre Genre { get; set; }
    }
}
