using NoiseVault.Domain.Enums.Ensaio;

namespace NoiseVault.Application.Musicians
{
    public static class MusicianRules
    {
        public const int MaxInstruments = 5;
        public const int OtherNameMaxLength = 60;
        public const int InfluencesMaxLength = 300;
        public const int MaxSampleLinks = 4;
        public const int SampleLinkMaxLength = 300;
        public const int CardGenres = 2;

        private static readonly string[] SampleHosts =
        [
            "youtube.com",
            "youtu.be",
            "open.spotify.com",
            "spotify.com",
            "bandcamp.com",
            "soundcloud.com",
        ];

        public static bool IsValidInstrument(EInstrument instrument, string? otherName) =>
            Enum.IsDefined(instrument)
            && (instrument == EInstrument.Other
                ? otherName is { Length: > 0 and <= OtherNameMaxLength }
                : otherName == null);

        public static bool IsValidSampleLink(string url)
        {
            if (url.Length > SampleLinkMaxLength)
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return false;

            var host = uri.Host.ToLowerInvariant();

            if (host.StartsWith("www."))
                host = host[4..];
            else if (host.StartsWith("m."))
                host = host[2..];

            return SampleHosts.Any(allowed => host == allowed || host.EndsWith("." + allowed));
        }
    }
}
