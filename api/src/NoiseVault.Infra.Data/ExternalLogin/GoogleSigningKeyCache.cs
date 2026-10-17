using Microsoft.IdentityModel.Tokens;

namespace NoiseVault.Infra.Data.ExternalLogin
{
    public class GoogleSigningKeyCache
    {
        private const string CertsUrl = "https://www.googleapis.com/oauth2/v3/certs";
        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(6);
        private static readonly TimeSpan MinimumRefreshInterval = TimeSpan.FromMinutes(1);

        private readonly SemaphoreSlim _lock = new(1, 1);
        private IReadOnlyList<SecurityKey> _keys = [];
        private DateTime _fetchedAt = DateTime.MinValue;

        public async Task<IReadOnlyList<SecurityKey>> GetAsync(HttpClient http, string? requiredKeyId, CancellationToken cancellationToken)
        {
            if (IsUsable(requiredKeyId))
                return _keys;

            await _lock.WaitAsync(cancellationToken);

            try
            {
                if (IsUsable(requiredKeyId) || DateTime.UtcNow - _fetchedAt < MinimumRefreshInterval)
                    return _keys;

                using var response = await http.GetAsync(CertsUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                _keys = new JsonWebKeySet(json).GetSigningKeys().ToList();
                _fetchedAt = DateTime.UtcNow;

                return _keys;
            }
            finally
            {
                _lock.Release();
            }
        }

        private bool IsUsable(string? requiredKeyId) =>
            DateTime.UtcNow - _fetchedAt < Lifetime
            && (requiredKeyId == null || _keys.Any(k => k.KeyId == requiredKeyId));
    }
}
