using Microsoft.AspNetCore.Authorization;

namespace NoiseVault.Api.Authorization
{
    public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
}
