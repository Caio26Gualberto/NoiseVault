namespace NoiseVault.Domain.Entities
{
    public class Band : EntityBase
    {
        public const int DefaultBannerPosition = 50;

        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? CityId { get; set; }
        public City? City { get; set; }
        public string? CityName { get; set; }
        public int? FoundedYear { get; set; }
        public string? InstagramUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public string? SpotifyUrl { get; set; }
        public bool IsVerified { get; set; }
        public string? SpotifyArtistId { get; set; }
        public DateTime? DiscographySyncedAt { get; set; }
        public string? AvatarKey { get; set; }
        public string? BannerKey { get; set; }
        public int BannerPosition { get; set; } = DefaultBannerPosition;

        public List<BandGenre> Genres { get; set; } = [];
        public List<BandMember> Members { get; set; } = [];
    }
}
