using Headbanger.Domain.Enums.Profiles;

namespace Headbanger.Application.Profiles
{
    public enum ESocialNetwork
    {
        Instagram = 0,
        Youtube = 1,
        Spotify = 2,
    }

    public static class ProfileRules
    {
        public const int BioMaxLength = 500;
        public const int InstrumentMaxLength = 60;
        public const int SocialLinkMaxLength = 300;
        public const int MaxGenres = 10;
        public const int MinBannerPosition = 0;
        public const int MaxBannerPosition = 100;

        private static readonly Dictionary<ESocialNetwork, string[]> AllowedHosts = new()
        {
            [ESocialNetwork.Instagram] = ["instagram.com"],
            [ESocialNetwork.Youtube] = ["youtube.com", "youtu.be"],
            [ESocialNetwork.Spotify] = ["open.spotify.com", "spotify.com"],
        };

        public static string? NormalizeOptionalText(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        public static bool IsValidCity(EAbcCity? city) => city is null || Enum.IsDefined(city.Value);

        public static bool IsValidGenres(IReadOnlyCollection<EMusicGenre> genres) =>
            genres.Count <= MaxGenres
            && genres.All(Enum.IsDefined)
            && genres.Distinct().Count() == genres.Count;

        public static bool IsValidBannerPosition(int position) =>
            position is >= MinBannerPosition and <= MaxBannerPosition;

        public static bool IsValidSocialLink(ESocialNetwork network, string? url)
        {
            if (url is null)
                return true;

            if (url.Length > SocialLinkMaxLength)
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                return false;

            var host = uri.Host.ToLowerInvariant();
            if (host.StartsWith("www."))
                host = host[4..];
            else if (host.StartsWith("m."))
                host = host[2..];

            return AllowedHosts[network].Contains(host);
        }
    }
}
