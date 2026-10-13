using Headbanger.Domain.Authorization;

namespace Headbanger.Application.Abstractions.Authorization
{
    public interface IPermissionDefinitionProvider
    {
        IReadOnlyList<PermissionGroupDefinition> GetGroups();
        IReadOnlyList<PermissionDefinition> GetPermissions();
        bool Exists(string name);
    }
}
