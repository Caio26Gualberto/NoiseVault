namespace NoiseVault.Application.Abstractions.Authorization
{
    public sealed record RoleRef(int Id, string Name);

    public sealed record UserPermissionsSnapshot(
        IReadOnlyList<RoleRef> Roles,
        IReadOnlyList<string> RolePermissions,
        IReadOnlyList<string> UserPermissions,
        IReadOnlyList<string> EffectivePermissions);

    public interface IPermissionChecker
    {
        Task<bool> IsGrantedAsync(int userId, string permission, CancellationToken ct = default);
        Task<UserPermissionsSnapshot?> GetUserPermissionsAsync(int userId, CancellationToken ct = default);
        Task<List<int>> UsersGrantedAsync(string permission, CancellationToken ct = default);
        Task InvalidateUserAsync(int userId, CancellationToken ct = default);
        Task InvalidateRoleAsync(int roleId, CancellationToken ct = default);
    }
}
