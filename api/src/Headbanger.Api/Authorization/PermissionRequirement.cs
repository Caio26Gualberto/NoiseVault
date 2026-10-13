using Microsoft.AspNetCore.Authorization;

namespace Headbanger.Api.Authorization
{
    public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
}
