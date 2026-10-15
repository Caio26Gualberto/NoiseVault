using NoiseVault.Application.Auth;

namespace NoiseVault.Api.Authentication
{
    public sealed class RefreshCookie
    {
        public const string Name = "nv_refresh";
        private const string CookiePath = "/api/auth";

        private readonly string? _domain;

        public RefreshCookie(IConfiguration configuration)
        {
            var domain = configuration["Auth:RefreshCookie:Domain"];
            _domain = string.IsNullOrWhiteSpace(domain) ? null : domain;
        }

        public string Read(HttpRequest request) => request.Cookies[Name] ?? string.Empty;

        public void Write(HttpResponse response, string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return;

            response.Cookies.Append(Name, refreshToken, Options(DateTimeOffset.UtcNow.Add(AuthTokenRules.RefreshTokenLifetime)));
        }

        public void Clear(HttpResponse response) => response.Cookies.Delete(Name, Options(null));

        private CookieOptions Options(DateTimeOffset? expires) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            Domain = _domain,
            Expires = expires,
            IsEssential = true,
        };
    }
}
