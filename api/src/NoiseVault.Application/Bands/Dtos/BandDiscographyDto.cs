using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandTrackDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DiscNumber { get; set; }
        public int Number { get; set; }
        public int? DurationSeconds { get; set; }
        public string? SpotifyTrackId { get; set; }
        public string? ExternalUrl { get; set; }
        public bool IsFeatured { get; set; }
    }

    public class BandReleaseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public EReleaseKind Kind { get; set; }
        public int Year { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? CoverUrl { get; set; }
        public EReleaseSource Source { get; set; }
        public string? SpotifyAlbumId { get; set; }
        public string? ExternalUrl { get; set; }
        public int Order { get; set; }
        public bool IsHidden { get; set; }
        public List<BandTrackDto> Tracks { get; set; } = [];
    }

    public class FeaturedTrackDto
    {
        public BandTrackDto Track { get; set; } = new();
        public int ReleaseId { get; set; }
        public string ReleaseTitle { get; set; } = string.Empty;
        public string? CoverUrl { get; set; }
    }

    public class BandDiscographyDto
    {
        public DateTime? SyncedAt { get; set; }
        public string? SpotifyArtistId { get; set; }
        public string? SpotifyArtistUrl { get; set; }
        public bool CanEdit { get; set; }
        public bool CanImport { get; set; }
        public FeaturedTrackDto? FeaturedTrack { get; set; }
        public List<BandReleaseDto> Releases { get; set; } = [];
    }

    public class BandTrackInputDto
    {
        public string? Title { get; set; }
        public string? ExternalUrl { get; set; }
        public int? DurationSeconds { get; set; }
    }

    public class BandReleaseInputDto
    {
        public string? Title { get; set; }
        public EReleaseKind Kind { get; set; }
        public int Year { get; set; }
        public string? ExternalUrl { get; set; }
        public List<BandTrackInputDto>? Tracks { get; set; }
    }

    public class DiscographyOrderInputDto
    {
        public List<int>? ReleaseIds { get; set; }
    }

    public class ReleaseVisibilityInputDto
    {
        public bool Hidden { get; set; }
    }

    public class FeaturedTrackInputDto
    {
        public int? TrackId { get; set; }
    }
}
