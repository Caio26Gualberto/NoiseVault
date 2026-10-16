using Microsoft.Extensions.Configuration;

namespace NoiseVault.Infra.Data.Spotify
{
    public class SpotifySettings
    {
        public string ClientId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty;
        public string Market { get; init; } = "BR";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

        public static SpotifySettings FromConfiguration(IConfiguration config) => new()
        {
            ClientId = config["Spotify:ClientId"] ?? string.Empty,
            ClientSecret = config["Spotify:ClientSecret"] ?? string.Empty,
            Market = config["Spotify:Market"] is { Length: > 0 } market ? market : "BR",
        };
    }
}
