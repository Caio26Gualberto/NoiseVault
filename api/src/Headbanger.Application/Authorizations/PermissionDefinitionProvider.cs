using Headbanger.Application.Abstractions.Authorization;
using Headbanger.Domain.Authorization;

namespace Headbanger.Application.Authorizations
{
    public class PermissionDefinitionProvider : IPermissionDefinitionProvider
    {
        public IReadOnlyList<PermissionGroupDefinition> GetGroups() => PermissionCatalog.GroupDefinitions;

        public IReadOnlyList<PermissionDefinition> GetPermissions() => PermissionCatalog.All;

        public bool Exists(string name) => PermissionCatalog.Exists(name);
    }
}
