namespace Headbanger.Application.Abstractions.Authorization
{
    public sealed record PermissionChangeResult(
        bool Succeeded,
        string? ErrorCode = null,
        IReadOnlyList<string>? Before = null,
        IReadOnlyList<string>? After = null)
    {
        public static PermissionChangeResult Ok(IReadOnlyList<string> before, IReadOnlyList<string> after) =>
            new(true, null, before, after);

        public static PermissionChangeResult Fail(string errorCode) => new(false, errorCode);
    }

    public interface IPermissionManager
    {
        Task<IReadOnlyList<string>?> GetRolePermissionsAsync(int roleId, CancellationToken ct = default);
        Task<PermissionChangeResult> SetRolePermissionsAsync(int roleId, IReadOnlyList<string> permissions, CancellationToken ct = default);
        Task<IReadOnlyList<string>?> GetUserAdditionalPermissionsAsync(int userId, CancellationToken ct = default);
        Task<PermissionChangeResult> SetUserAdditionalPermissionsAsync(int userId, IReadOnlyList<string> permissions, CancellationToken ct = default);
    }
}
