using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Interfaces;

namespace NoiseVault.Infra.Data.Spotify
{
    public class SpotifyCatalogClient : ISpotifyCatalog
    {
        private const string TokenUrl = "https://accounts.spotify.com/api/token";
        private const string ApiBase = "https://api.spotify.com/v1/";
        private const int AlbumsPageSize = 50;
        private const int AlbumsBatchSize = 20;
        private const int TracksPageSize = 50;

        private readonly HttpClient _http;
        private readonly SpotifySettings _settings;
        private readonly SpotifyTokenCache _tokens;
        private readonly IApplicationLogger _logger;

        public SpotifyCatalogClient(HttpClient http, SpotifySettings settings, SpotifyTokenCache tokens, IApplicationLoggerFactory loggerFactory)
        {
            _http = http;
            _settings = settings;
            _tokens = tokens;
            _logger = loggerFactory.Create(LogContext.Bands);
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task<SpotifyArtist> GetArtistAsync(string artistId, CancellationToken cancellationToken = default)
        {
            using var document = await GetJsonAsync($"{ApiBase}artists/{Uri.EscapeDataString(artistId)}", cancellationToken);
            var root = document.RootElement;

            return new SpotifyArtist(
                root.GetProperty("id").GetString()!,
                root.GetProperty("name").GetString() ?? string.Empty,
                ExternalUrl(root));
        }

        public async Task<IReadOnlyList<SpotifyAlbum>> GetArtistAlbumsAsync(string artistId, CancellationToken cancellationToken = default)
        {
            var summaries = new List<(string Id, string Name, string Type, string ReleaseDate, string Precision, string? Cover, string Url, int TotalTracks)>();
            var next = $"{ApiBase}artists/{Uri.EscapeDataString(artistId)}/albums?include_groups=album,single&market={Uri.EscapeDataString(_settings.Market)}&limit={AlbumsPageSize}";

            while (next != null)
            {
                using var document = await GetJsonAsync(next, cancellationToken);
                var root = document.RootElement;

                foreach (var item in root.GetProperty("items").EnumerateArray())
                {
                    summaries.Add((
                        item.GetProperty("id").GetString()!,
                        item.GetProperty("name").GetString() ?? string.Empty,
                        item.GetProperty("album_type").GetString() ?? "album",
                        item.GetProperty("release_date").GetString() ?? string.Empty,
                        item.GetProperty("release_date_precision").GetString() ?? "year",
                        LargestImage(item),
                        ExternalUrl(item),
                        item.TryGetProperty("total_tracks", out var total) ? total.GetInt32() : 0));
                }

                next = root.TryGetProperty("next", out var nextElement) && nextElement.ValueKind == JsonValueKind.String ? nextElement.GetString() : null;
            }

            var albums = new List<SpotifyAlbum>(summaries.Count);

            foreach (var batch in summaries.Chunk(AlbumsBatchSize))
            {
                var ids = string.Join(",", batch.Select(b => b.Id));
                using var document = await GetJsonAsync($"{ApiBase}albums?ids={Uri.EscapeDataString(ids)}&market={Uri.EscapeDataString(_settings.Market)}", cancellationToken);

                foreach (var album in document.RootElement.GetProperty("albums").EnumerateArray())
                {
                    if (album.ValueKind != JsonValueKind.Object)
                        continue;

                    var id = album.GetProperty("id").GetString()!;
                    var summary = batch.First(b => b.Id == id);
                    var tracks = ReadTracks(album.GetProperty("tracks"));

                    if (album.GetProperty("tracks").TryGetProperty("next", out var moreTracks) && moreTracks.ValueKind == JsonValueKind.String)
                        tracks.AddRange(await ReadRemainingTracksAsync(moreTracks.GetString()!, cancellationToken));

                    albums.Add(new SpotifyAlbum(id, summary.Name, summary.Type, summary.ReleaseDate, summary.Precision, summary.Cover, summary.Url, summary.TotalTracks, tracks));
                }
            }

            return albums;
        }

        private async Task<List<SpotifyTrack>> ReadRemainingTracksAsync(string next, CancellationToken cancellationToken)
        {
            var tracks = new List<SpotifyTrack>();
            string? url = next;

            while (url != null)
            {
                using var document = await GetJsonAsync(url, cancellationToken);
                tracks.AddRange(ReadTracks(document.RootElement));
                url = document.RootElement.TryGetProperty("next", out var nextElement) && nextElement.ValueKind == JsonValueKind.String ? nextElement.GetString() : null;
            }

            return tracks;
        }

        private static List<SpotifyTrack> ReadTracks(JsonElement page)
        {
            var tracks = new List<SpotifyTrack>();

            foreach (var item in page.GetProperty("items").EnumerateArray())
            {
                tracks.Add(new SpotifyTrack(
                    item.GetProperty("id").GetString()!,
                    item.GetProperty("name").GetString() ?? string.Empty,
                    item.TryGetProperty("disc_number", out var disc) ? disc.GetInt32() : 1,
                    item.TryGetProperty("track_number", out var number) ? number.GetInt32() : tracks.Count + 1,
                    item.TryGetProperty("duration_ms", out var duration) ? duration.GetInt32() : 0,
                    ExternalUrl(item)));
            }

            return tracks;
        }

        private static string ExternalUrl(JsonElement element) =>
            element.TryGetProperty("external_urls", out var urls) && urls.TryGetProperty("spotify", out var url)
                ? url.GetString() ?? string.Empty
                : string.Empty;

        private static string? LargestImage(JsonElement element)
        {
            if (!element.TryGetProperty("images", out var images) || images.ValueKind != JsonValueKind.Array)
                return null;

            string? best = null;
            var bestWidth = -1;

            foreach (var image in images.EnumerateArray())
            {
                var width = image.TryGetProperty("width", out var w) && w.ValueKind == JsonValueKind.Number ? w.GetInt32() : 0;

                if (width > bestWidth && image.TryGetProperty("url", out var url))
                {
                    best = url.GetString();
                    bestWidth = width;
                }
            }

            return best;
        }

        private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken cancellationToken)
        {
            if (!IsConfigured)
                throw new SpotifyCatalogException(ESpotifyFailure.NotConfigured, "Spotify:ClientId e Spotify:ClientSecret não estão configurados.");

            var retried = false;

            while (true)
            {
                var token = await _tokens.GetOrRefreshAsync(RequestTokenAsync, cancellationToken);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response;

                try
                {
                    response = await _http.SendAsync(request, cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    _logger.Error("SPOTIFY_REQUEST_FAILED", "Spotify request failed", exception: ex, extra: new { Path = PathOf(url) });
                    throw new SpotifyCatalogException(ESpotifyFailure.Unavailable, "O Spotify não respondeu.", ex);
                }

                using (response)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized && !retried)
                    {
                        retried = true;
                        _tokens.Invalidate();
                        continue;
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests && !retried)
                    {
                        retried = true;
                        var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                        await Task.Delay(wait > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : wait, cancellationToken);
                        continue;
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        throw new SpotifyCatalogException(ESpotifyFailure.RateLimited, "O Spotify limitou as chamadas do app.");

                    if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest)
                        throw new SpotifyCatalogException(ESpotifyFailure.NotFound, "Artista não encontrado no Spotify.");

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.Error("SPOTIFY_REQUEST_FAILED", "Spotify answered with an error status", extra: new { Path = PathOf(url), Status = (int)response.StatusCode });
                        throw new SpotifyCatalogException(ESpotifyFailure.Unavailable, $"O Spotify respondeu {(int)response.StatusCode}.");
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                }
            }
        }

        private async Task<(string Token, int ExpiresInSeconds)> RequestTokenAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("grant_type", "client_credentials")]);

            HttpResponseMessage response;

            try
            {
                response = await _http.SendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.Error("SPOTIFY_REQUEST_FAILED", "Spotify token request failed", exception: ex);
                throw new SpotifyCatalogException(ESpotifyFailure.Unavailable, "O Spotify não respondeu ao pedido de token.", ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("SPOTIFY_REQUEST_FAILED", "Spotify refused the app credentials", extra: new { Status = (int)response.StatusCode });
                    throw new SpotifyCatalogException(ESpotifyFailure.Unavailable, $"O Spotify recusou as credenciais do app ({(int)response.StatusCode}).");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = document.RootElement;

                return (root.GetProperty("access_token").GetString()!, root.TryGetProperty("expires_in", out var expires) ? expires.GetInt32() : 3600);
            }
        }

        private static string PathOf(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
    }
}
