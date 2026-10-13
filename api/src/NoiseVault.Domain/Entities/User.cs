using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Domain.Entities
{
    public class User : EntityBase
    {
        public const int DefaultBannerPosition = 50;

        public string Name { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public string? Bio { get; set; }
        public EAbcCity? City { get; set; }
        public string? MainInstrument { get; set; }
        public string? InstagramUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public string? SpotifyUrl { get; set; }
        public string? AvatarKey { get; set; }
        public string? BannerKey { get; set; }
        public int BannerPosition { get; set; } = DefaultBannerPosition;

        public List<UserGenre> Genres { get; set; } = [];
    }
}
