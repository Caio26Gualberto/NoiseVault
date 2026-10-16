using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Domain.Entities
{
    public class BandRelease : EntityBase
    {
        public int BandId { get; set; }
        public string Title { get; set; } = string.Empty;
        public EReleaseKind Kind { get; set; }
        public int Year { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? CoverUrl { get; set; }
        public string? CoverKey { get; set; }
        public EReleaseSource Source { get; set; }
        public string? SpotifyAlbumId { get; set; }
        public string? ExternalUrl { get; set; }
        public int Order { get; set; }
        public bool IsHidden { get; set; }
        public Band? Band { get; set; }
        public List<BandTrack> Tracks { get; set; } = [];
    }
}
