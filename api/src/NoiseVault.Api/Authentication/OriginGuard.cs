namespace NoiseVault.Api.Authentication
{
    public sealed class OriginGuard
    {
        private readonly HashSet<string> _allowed;

        public OriginGuard(IConfiguration configuration)
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            _allowed = origins.Select(o => o.TrimEnd('/')).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public bool IsAllowed(HttpRequest request)
        {
            var origin = request.Headers.Origin.ToString();
            return string.IsNullOrEmpty(origin) || _allowed.Contains(origin.TrimEnd('/'));
        }
    }
}
