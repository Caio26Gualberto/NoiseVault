namespace NoiseVault.Application.Auth
{
    public static class AuthTokenRules
    {
        public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    }
}
