using NoiseVault.Application.Abstractions.Authentication;

namespace NoiseVault.Infra.Data.Context.Factory
{
    public class DesignTimeCurrentContextService : ICurrentUser
    {
        public int UserId => 0;
        public bool IsAuthenticated => false;
        public string Email => string.Empty;
        public string Name => string.Empty;
        public string? Role => null;
        public int? UserIdOrNull => null;
        public string? IpAddress => null;
    }
}
