using System.Text.RegularExpressions;

namespace NoiseVault.Application.Bands
{
    public static partial class DiscographyRules
    {
        public const int TitleMinLength = 1;
        public const int TitleMaxLength = 200;
        public const int MinYear = 1950;
        public const int MaxReleasesPerBand = 50;
        public const int MaxTracksPerRelease = 100;
        public const int ExternalUrlMaxLength = 500;

        public static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(24);

        public static string? ParseSpotifyArtistId(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var trimmed = url.Trim();
            var uri = UriPattern().Match(trimmed);

            if (uri.Success)
                return uri.Groups["id"].Value;

            var link = LinkPattern().Match(trimmed);

            return link.Success ? link.Groups["id"].Value : null;
        }

        public static bool IsValidExternalUrl(string? url) =>
            url is null
            || (url.Length <= ExternalUrlMaxLength
                && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

        public static string NormalizeTitle(string? title) =>
            Spaces().Replace(title?.Trim() ?? string.Empty, " ");

        public static string DedupeKey(string title, int year) =>
            $"{Spaces().Replace(title.Trim().ToLowerInvariant(), " ")}|{year}";

        [GeneratedRegex(@"^spotify:artist:(?<id>[A-Za-z0-9]{22})$")]
        private static partial Regex UriPattern();

        [GeneratedRegex(@"^https?://(?:www\.|m\.)?open\.spotify\.com/(?:intl-[a-z]{2}(?:-[a-z]{2})?/)?artist/(?<id>[A-Za-z0-9]{22})(?:[/?#].*)?$", RegexOptions.IgnoreCase)]
        private static partial Regex LinkPattern();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Spaces();
    }
}
