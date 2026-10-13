using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Domain.Authorization;

namespace NoiseVault.Application.Authorizations
{
    public class PermissionDefinitionProvider : IPermissionDefinitionProvider
    {
        public IReadOnlyList<PermissionGroupDefinition> GetGroups() => PermissionCatalog.GroupDefinitions;

        public IReadOnlyList<PermissionDefinition> GetPermissions() => PermissionCatalog.All;

        public bool Exists(string name) => PermissionCatalog.Exists(name);
    }
}
