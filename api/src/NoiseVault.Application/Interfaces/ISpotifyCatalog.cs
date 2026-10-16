namespace NoiseVault.Application.Interfaces
{
    public sealed record SpotifyArtist(string Id, string Name, string Url);

    public sealed record SpotifyTrack(string Id, string Name, int DiscNumber, int Number, int DurationMs, string Url);

    public sealed record SpotifyAlbum(
        string Id,
        string Name,
        string AlbumType,
        string ReleaseDate,
        string ReleaseDatePrecision,
        string? CoverUrl,
        string Url,
        int TotalTracks,
        IReadOnlyList<SpotifyTrack> Tracks);

    public enum ESpotifyFailure
    {
        NotConfigured = 0,
        NotFound = 1,
        RateLimited = 2,
        Unavailable = 3,
    }

    public sealed class SpotifyCatalogException : Exception
    {
        public ESpotifyFailure Failure { get; }

        public SpotifyCatalogException(ESpotifyFailure failure, string message, Exception? inner = null) : base(message, inner)
        {
            Failure = failure;
        }
    }

    public interface ISpotifyCatalog
    {
        bool IsConfigured { get; }
        Task<SpotifyArtist> GetArtistAsync(string artistId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<SpotifyAlbum>> GetArtistAlbumsAsync(string artistId, CancellationToken cancellationToken = default);
    }
}
