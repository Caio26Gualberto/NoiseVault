using NoiseVault.Domain.Authorization;

namespace NoiseVault.Application.Abstractions.Authorization
{
    public interface IPermissionDefinitionProvider
    {
        IReadOnlyList<PermissionGroupDefinition> GetGroups();
        IReadOnlyList<PermissionDefinition> GetPermissions();
        bool Exists(string name);
    }
}
