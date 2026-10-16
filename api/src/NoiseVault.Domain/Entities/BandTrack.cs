namespace NoiseVault.Domain.Entities
{
    public class BandTrack : EntityBase
    {
        public int BandReleaseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DiscNumber { get; set; } = 1;
        public int Number { get; set; }
        public int? DurationSeconds { get; set; }
        public string? SpotifyTrackId { get; set; }
        public string? ExternalUrl { get; set; }
        public bool IsFeatured { get; set; }
        public BandRelease? Release { get; set; }
    }
}
