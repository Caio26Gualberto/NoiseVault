using NoiseVault.Domain.Enums.Auth;

namespace NoiseVault.Domain.Interfaces.Authenticate
{
    public sealed record SignInMethods(bool HasPassword, bool EmailConfirmed, IReadOnlyList<EExternalProvider> Providers);
}
