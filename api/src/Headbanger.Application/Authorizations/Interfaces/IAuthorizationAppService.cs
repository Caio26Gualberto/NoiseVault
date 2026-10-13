using Headbanger.Application.Authorizations.Dtos;
using Headbanger.Application.Response;

namespace Headbanger.Application.Authorizations.Interfaces
{
    public interface IAuthorizationAppService
    {
        Task<Result<List<PermissionGroupDto>>> GetPermissionCatalog();
        Task<Result<List<RoleDto>>> ListRoles();
        Task<Result<RoleDto>> GetRole(int roleId);
        Task<Result<RoleDto>> CreateRole(CreateRoleRequestDto request);
        Task<Result<RoleDto>> UpdateRole(int roleId, UpdateRoleRequestDto request);
        Task<Result> DeleteRole(int roleId);
        Task<Result<List<string>>> GetRolePermissions(int roleId);
        Task<Result<List<string>>> SetRolePermissions(int roleId, RolePermissionsRequestDto request);
        Task<Result<List<AdminUserDto>>> ListUsers();
        Task<Result<UserAccessDto>> GetUserAccess(int userId);
        Task<Result<RoleChangePreviewDto>> GetRoleChangePreview(int userId, int roleId);
        Task<Result<UserAccessDto>> UpdateUserRole(int userId, UpdateUserRoleRequestDto request);
        Task<Result<UserAccessDto>> UpdateUserPermissions(int userId, UpdateUserPermissionsRequestDto request);
        Task<Result<MyAccessDto>> Me();
        Task<Result<AuditLogListDto>> ListAuditLogs(int page, int pageSize);
    }
}
