namespace NoiseVault.Application.Abstractions.Authorization
{
    public sealed record RoleInfo(
        int Id,
        string Name,
        string? Description,
        bool IsSystem,
        bool IsDefault,
        int UserCount,
        int PermissionCount);

    public sealed record UserAccessInfo(
        int UserId,
        string Name,
        string Email,
        string Role);

    public sealed record RoleOpResult(
        bool Succeeded,
        string? ErrorCode = null,
        RoleInfo? Role = null,
        int UsersInRole = 0)
    {
        public static RoleOpResult Ok(RoleInfo? role = null) => new(true, null, role);

        public static RoleOpResult Fail(string errorCode, int usersInRole = 0) => new(false, errorCode, null, usersInRole);
    }

    public sealed record RevokeAccessResult(
        bool Succeeded,
        string? ErrorCode = null,
        IReadOnlyList<string>? Roles = null,
        IReadOnlyList<string>? Permissions = null)
    {
        public static RevokeAccessResult Ok(IReadOnlyList<string> roles, IReadOnlyList<string> permissions) =>
            new(true, null, roles, permissions);

        public static RevokeAccessResult Fail(string errorCode) => new(false, errorCode);
    }

    public interface IRoleManagementService
    {
        Task<IReadOnlyList<RoleInfo>> ListRolesAsync(CancellationToken ct = default);
        Task<RoleInfo?> GetRoleAsync(int roleId, CancellationToken ct = default);
        Task<RoleOpResult> CreateRoleAsync(string name, string? description, CancellationToken ct = default);
        Task<RoleOpResult> UpdateRoleAsync(int roleId, string name, string? description, CancellationToken ct = default);
        Task<RoleOpResult> DeleteRoleAsync(int roleId, CancellationToken ct = default);
        Task<RoleOpResult> AssignUserRoleAsync(int userId, int roleId, CancellationToken ct = default);
        Task<RevokeAccessResult> RevokeUserAccessAsync(int userId, CancellationToken ct = default);
        Task<IReadOnlyList<UserAccessInfo>> ListUsersWithRolesAsync(CancellationToken ct = default);
    }
}
