namespace NoiseVault.Domain.Authorization
{
    public sealed record PermissionDefinition(
        string Name,
        string DisplayName,
        string Description,
        string Group,
        string? ParentName = null);
}
