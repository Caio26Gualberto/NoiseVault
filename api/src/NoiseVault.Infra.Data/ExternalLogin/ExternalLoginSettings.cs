using Microsoft.Extensions.Configuration;

namespace NoiseVault.Infra.Data.ExternalLogin
{
    public class ExternalLoginSettings
    {
        public string GoogleClientId { get; init; } = string.Empty;
        public string FacebookAppId { get; init; } = string.Empty;
        public string FacebookAppSecret { get; init; } = string.Empty;

        public bool IsGoogleConfigured => !string.IsNullOrWhiteSpace(GoogleClientId);
        public bool IsFacebookConfigured => !string.IsNullOrWhiteSpace(FacebookAppId) && !string.IsNullOrWhiteSpace(FacebookAppSecret);

        public static ExternalLoginSettings FromConfiguration(IConfiguration config) => new()
        {
            GoogleClientId = config["Auth:Google:ClientId"] ?? string.Empty,
            FacebookAppId = config["Auth:Facebook:AppId"] ?? string.Empty,
            FacebookAppSecret = config["Auth:Facebook:AppSecret"] ?? string.Empty,
        };
    }
}
