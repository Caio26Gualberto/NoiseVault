using Microsoft.Extensions.Configuration;

namespace NoiseVault.Infra.Data.News
{
    public class WhiplashNewsSettings
    {
        public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

        public bool Enabled { get; init; } = true;
        public string UserAgent { get; init; } = DefaultUserAgent;

        public static WhiplashNewsSettings FromConfiguration(IConfiguration config) => new()
        {
            Enabled = !bool.TryParse(config["News:Enabled"], out var enabled) || enabled,
            UserAgent = config["News:UserAgent"] is { Length: > 0 } userAgent ? userAgent : DefaultUserAgent,
        };
    }
}
