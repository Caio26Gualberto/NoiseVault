using Headbanger.Application.Abstractions.Authentication;
using Headbanger.Application.Abstractions.Authorization;
using Headbanger.Application.Abstractions.Repository;
using Headbanger.Application.Authorizations.Dtos;
using Headbanger.Application.Authorizations.Interfaces;
using Headbanger.Application.Response;
using Headbanger.Domain.Constants;
using Headbanger.Domain.Entities;
using System.Text.Json;

namespace Headbanger.Application.Authorizations
{
    public class AuthorizationAppService : IAuthorizationAppService
    {
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionDefinitionProvider _definitionProvider;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IPermissionManager _permissionManager;
        private readonly IRoleManagementService _roleManagementService;
        private readonly IRepository<AuthorizationAuditLog> _auditRepository;

        public AuthorizationAppService(
            ICurrentUser currentUser,
            IPermissionDefinitionProvider definitionProvider,
            IPermissionChecker permissionChecker,
            IPermissionManager permissionManager,
            IRoleManagementService roleManagementService,
            IRepository<AuthorizationAuditLog> auditRepository)
        {
            _currentUser = currentUser;
            _definitionProvider = definitionProvider;
            _permissionChecker = permissionChecker;
            _permissionManager = permissionManager;
            _roleManagementService = roleManagementService;
            _auditRepository = auditRepository;
        }

        public Task<Result<List<PermissionGroupDto>>> GetPermissionCatalog()
        {
            var permissions = _definitionProvider.GetPermissions();

            var groups = _definitionProvider.GetGroups()
                .Select(g => new PermissionGroupDto
                {
                    Name = g.Name,
                    DisplayName = g.DisplayName,
                    Permissions = permissions
                        .Where(p => p.Group == g.Name)
                        .Select(p => new PermissionDto
                        {
                            Name = p.Name,
                            DisplayName = p.DisplayName,
                            Description = p.Description,
                            Group = p.Group,
                            ParentName = p.ParentName,
                        })
                        .ToList(),
                })
                .Where(g => g.Permissions.Count > 0)
                .ToList();

            return Task.FromResult(Result<List<PermissionGroupDto>>.Ok(groups, "PERMISSIONS_FOUND", "Permissions catalog retrieved successfully"));
        }

        public async Task<Result<List<RoleDto>>> ListRoles()
        {
            var roles = await _roleManagementService.ListRolesAsync();
            var dtos = roles.Select(ToRoleDto).ToList();
            return Result<List<RoleDto>>.Ok(dtos, "ROLES_FOUND", "Roles retrieved successfully");
        }

        public async Task<Result<RoleDto>> GetRole(int roleId)
        {
            var role = await _roleManagementService.GetRoleAsync(roleId);
            if (role is null)
                return Result<RoleDto>.Fail(MapError(AuthorizationErrorCodes.RoleNotFound));

            return Result<RoleDto>.Ok(ToRoleDto(role), "ROLE_FOUND", "Role retrieved successfully");
        }

        public async Task<Result<RoleDto>> CreateRole(CreateRoleRequestDto request)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                return Result<RoleDto>.Fail(new Error("ROLE_NAME_REQUIRED", "Role name is required", ErrorType.Validation));

            var result = await _roleManagementService.CreateRoleAsync(name, request.Description?.Trim());
            if (!result.Succeeded || result.Role is null)
                return Result<RoleDto>.Fail(MapError(result.ErrorCode!));

            var permissions = new List<string>();
            if (request.Permissions.Count > 0)
            {
                var changeResult = await _permissionManager.SetRolePermissionsAsync(result.Role.Id, request.Permissions);
                if (!changeResult.Succeeded)
                {
                    await _roleManagementService.DeleteRoleAsync(result.Role.Id);
                    return Result<RoleDto>.Fail(MapError(changeResult.ErrorCode!));
                }

                permissions = changeResult.After!.ToList();
            }

            await WriteAuditAsync(
                AuthorizationAuditActions.RoleCreated,
                "Role",
                result.Role.Id.ToString(),
                result.Role.Name,
                null,
                new { result.Role.Name, result.Role.Description, Permissions = permissions });

            var created = await _roleManagementService.GetRoleAsync(result.Role.Id);
            return Result<RoleDto>.Ok(ToRoleDto(created ?? result.Role), "ROLE_CREATED", "Role created successfully");
        }

        public async Task<Result<RoleDto>> UpdateRole(int roleId, UpdateRoleRequestDto request)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                return Result<RoleDto>.Fail(new Error("ROLE_NAME_REQUIRED", "Role name is required", ErrorType.Validation));

            var before = await _roleManagementService.GetRoleAsync(roleId);
            if (before is null)
                return Result<RoleDto>.Fail(MapError(AuthorizationErrorCodes.RoleNotFound));

            var result = await _roleManagementService.UpdateRoleAsync(roleId, name, request.Description?.Trim());
            if (!result.Succeeded || result.Role is null)
                return Result<RoleDto>.Fail(MapError(result.ErrorCode!));

            await WriteAuditAsync(
                AuthorizationAuditActions.RoleUpdated,
                "Role",
                roleId.ToString(),
                result.Role.Name,
                new { before.Name, before.Description },
                new { result.Role.Name, result.Role.Description });

            return Result<RoleDto>.Ok(ToRoleDto(result.Role), "ROLE_UPDATED", "Role updated successfully");
        }

        public async Task<Result> DeleteRole(int roleId)
        {
            var before = await _roleManagementService.GetRoleAsync(roleId);
            if (before is null)
                return Result.Fail(MapError(AuthorizationErrorCodes.RoleNotFound));

            var permissions = await _permissionManager.GetRolePermissionsAsync(roleId) ?? [];

            var result = await _roleManagementService.DeleteRoleAsync(roleId);
            if (!result.Succeeded)
                return Result.Fail(MapError(result.ErrorCode!, result.UsersInRole));

            await WriteAuditAsync(
                AuthorizationAuditActions.RoleDeleted,
                "Role",
                roleId.ToString(),
                before.Name,
                new { before.Name, before.Description, Permissions = permissions },
                null);

            return Result.Ok("ROLE_DELETED", "Role deleted successfully");
        }

        public async Task<Result<List<string>>> GetRolePermissions(int roleId)
        {
            var permissions = await _permissionManager.GetRolePermissionsAsync(roleId);
            if (permissions is null)
                return Result<List<string>>.Fail(MapError(AuthorizationErrorCodes.RoleNotFound));

            return Result<List<string>>.Ok(permissions.ToList(), "ROLE_PERMISSIONS_FOUND", "Role permissions retrieved successfully");
        }

        public async Task<Result<List<string>>> SetRolePermissions(int roleId, RolePermissionsRequestDto request)
        {
            var role = await _roleManagementService.GetRoleAsync(roleId);
            if (role is null)
                return Result<List<string>>.Fail(MapError(AuthorizationErrorCodes.RoleNotFound));

            var result = await _permissionManager.SetRolePermissionsAsync(roleId, request.Permissions);
            if (!result.Succeeded)
                return Result<List<string>>.Fail(MapError(result.ErrorCode!));

            await WriteAuditAsync(
                AuthorizationAuditActions.RolePermissionsChanged,
                "Role",
                roleId.ToString(),
                role.Name,
                new { Permissions = result.Before },
                new { Permissions = result.After });

            return Result<List<string>>.Ok(result.After!.ToList(), "ROLE_PERMISSIONS_UPDATED", "Role permissions updated successfully");
        }

        public async Task<Result<List<AdminUserDto>>> ListUsers()
        {
            var users = await _roleManagementService.ListUsersWithRolesAsync();
            var dtos = users.Select(u => new AdminUserDto
            {
                Id = u.UserId,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
            }).ToList();

            return Result<List<AdminUserDto>>.Ok(dtos, "USERS_FOUND", "Users retrieved successfully");
        }

        public async Task<Result<UserAccessDto>> GetUserAccess(int userId)
        {
            var snapshot = await _permissionChecker.GetUserPermissionsAsync(userId);
            if (snapshot is null)
                return Result<UserAccessDto>.Fail(MapError(AuthorizationErrorCodes.UserNotFound));

            var staff = await _roleManagementService.ListUsersWithRolesAsync();
            var user = staff.FirstOrDefault(s => s.UserId == userId);

            return Result<UserAccessDto>.Ok(
                ToUserAccessDto(userId, user?.Name ?? string.Empty, user?.Email ?? string.Empty, snapshot),
                "USER_ACCESS_FOUND",
                "User access retrieved successfully");
        }

        public async Task<Result<RoleChangePreviewDto>> GetRoleChangePreview(int userId, int roleId)
        {
            var snapshot = await _permissionChecker.GetUserPermissionsAsync(userId);
            if (snapshot is null)
                return Result<RoleChangePreviewDto>.Fail(MapError(AuthorizationErrorCodes.UserNotFound));

            var targetRole = await _roleManagementService.GetRoleAsync(roleId);
            if (targetRole is null)
                return Result<RoleChangePreviewDto>.Fail(MapError(AuthorizationErrorCodes.RoleNotFound));

            IReadOnlyList<string> targetPermissions = targetRole.Name == Roles.Admin
                ? Domain.Authorization.PermissionCatalog.AllNames
                : await _permissionManager.GetRolePermissionsAsync(roleId) ?? [];

            var currentEffective = snapshot.EffectivePermissions.ToHashSet(StringComparer.Ordinal);
            var newEffective = targetPermissions
                .Union(snapshot.UserPermissions, StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            var preview = new RoleChangePreviewDto
            {
                Added = newEffective.Except(currentEffective).OrderBy(p => p, StringComparer.Ordinal).ToList(),
                Removed = currentEffective.Except(newEffective).OrderBy(p => p, StringComparer.Ordinal).ToList(),
                Preserved = snapshot.UserPermissions.ToList(),
            };

            return Result<RoleChangePreviewDto>.Ok(preview, "ROLE_CHANGE_PREVIEW", "Role change preview generated successfully");
        }

        public async Task<Result<UserAccessDto>> UpdateUserRole(int userId, UpdateUserRoleRequestDto request)
        {
            var before = await _permissionChecker.GetUserPermissionsAsync(userId);
            if (before is null)
                return Result<UserAccessDto>.Fail(MapError(AuthorizationErrorCodes.UserNotFound));

            var result = await _roleManagementService.AssignUserRoleAsync(userId, request.RoleId);
            if (!result.Succeeded)
                return Result<UserAccessDto>.Fail(MapError(result.ErrorCode!));

            var after = await _permissionChecker.GetUserPermissionsAsync(userId);

            await WriteAuditAsync(
                AuthorizationAuditActions.UserRoleChanged,
                "User",
                userId.ToString(),
                await GetUserNameAsync(userId),
                new { Roles = before.Roles.Select(r => r.Name).ToList() },
                new { Roles = (after?.Roles ?? []).Select(r => r.Name).ToList() });

            var accessResult = await GetUserAccess(userId);
            if (accessResult.IsFailure)
                return accessResult;

            return Result<UserAccessDto>.Ok(accessResult.Data, "USER_ROLE_UPDATED", "User role updated successfully");
        }

        public async Task<Result<UserAccessDto>> UpdateUserPermissions(int userId, UpdateUserPermissionsRequestDto request)
        {
            var result = await _permissionManager.SetUserAdditionalPermissionsAsync(userId, request.Permissions);
            if (!result.Succeeded)
                return Result<UserAccessDto>.Fail(MapError(result.ErrorCode!));

            await WriteAuditAsync(
                AuthorizationAuditActions.UserPermissionsChanged,
                "User",
                userId.ToString(),
                await GetUserNameAsync(userId),
                new { Permissions = result.Before },
                new { Permissions = result.After });

            var accessResult = await GetUserAccess(userId);
            if (accessResult.IsFailure)
                return accessResult;

            return Result<UserAccessDto>.Ok(accessResult.Data, "USER_PERMISSIONS_UPDATED", "User permissions updated successfully");
        }

        public async Task<Result<MyAccessDto>> Me()
        {
            var snapshot = await _permissionChecker.GetUserPermissionsAsync(_currentUser.UserId);
            if (snapshot is null)
                return Result<MyAccessDto>.Fail(MapError(AuthorizationErrorCodes.UserNotFound));

            var dto = new MyAccessDto
            {
                Roles = snapshot.Roles.Select(r => new RoleRefDto { Id = r.Id, Name = r.Name }).ToList(),
                RolePermissions = snapshot.RolePermissions.ToList(),
                UserPermissions = snapshot.UserPermissions.ToList(),
                EffectivePermissions = snapshot.EffectivePermissions.ToList(),
            };

            return Result<MyAccessDto>.Ok(dto, "MY_ACCESS_FOUND", "Current user access retrieved successfully");
        }

        public async Task<Result<AuditLogListDto>> ListAuditLogs(int page, int pageSize)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var options = new QueryOptions<AuthorizationAuditLog>
            {
                OrderBy = q => q.OrderByDescending(a => a.CreatedAt),
                AsNoTracking = true,
                Skip = (page - 1) * pageSize,
                Take = pageSize,
            };

            var items = await _auditRepository.ListAsync(options);
            var total = await _auditRepository.CountAsync(new QueryOptions<AuthorizationAuditLog> { AsNoTracking = true });

            var dto = new AuditLogListDto
            {
                Items = items.Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    Action = a.Action,
                    ActorUserId = a.ActorUserId,
                    ActorEmail = a.ActorEmail,
                    TargetType = a.TargetType,
                    TargetId = a.TargetId,
                    TargetName = a.TargetName,
                    OldValue = a.OldValueJson,
                    NewValue = a.NewValueJson,
                    IpAddress = a.IpAddress,
                    CreatedAt = a.CreatedAt,
                }).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
            };

            return Result<AuditLogListDto>.Ok(dto, "AUDIT_LOGS_FOUND", "Audit logs retrieved successfully");
        }

        private async Task<string> GetUserNameAsync(int userId)
        {
            var staff = await _roleManagementService.ListUsersWithRolesAsync();
            return staff.FirstOrDefault(s => s.UserId == userId)?.Name ?? userId.ToString();
        }

        private async Task WriteAuditAsync(
            string action,
            string targetType,
            string targetId,
            string targetName,
            object? oldValue,
            object? newValue)
        {
            await _auditRepository.AddAsync(new AuthorizationAuditLog
            {
                Action = action,
                ActorUserId = _currentUser.UserId,
                ActorEmail = _currentUser.Email,
                TargetType = targetType,
                TargetId = targetId,
                TargetName = targetName,
                OldValueJson = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
                NewValueJson = newValue is null ? null : JsonSerializer.Serialize(newValue),
                IpAddress = _currentUser.IpAddress,
            });
        }

        private static UserAccessDto ToUserAccessDto(int userId, string name, string email, UserPermissionsSnapshot snapshot) =>
            new()
            {
                Id = userId,
                Name = name,
                Email = email,
                Roles = snapshot.Roles.Select(r => new RoleRefDto { Id = r.Id, Name = r.Name }).ToList(),
                RolePermissions = snapshot.RolePermissions.ToList(),
                UserPermissions = snapshot.UserPermissions.ToList(),
                EffectivePermissions = snapshot.EffectivePermissions.ToList(),
            };

        private static RoleDto ToRoleDto(RoleInfo role) =>
            new()
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystem = role.IsSystem,
                IsDefault = role.IsDefault,
                UserCount = role.UserCount,
                PermissionCount = role.PermissionCount,
            };

        private static Error MapError(string errorCode, int usersInRole = 0) => errorCode switch
        {
            AuthorizationErrorCodes.RoleNotFound => new Error(errorCode, "Role not found", ErrorType.NotFound),
            AuthorizationErrorCodes.UserNotFound => new Error(errorCode, "User not found", ErrorType.NotFound),
            AuthorizationErrorCodes.RoleNameTaken => new Error(errorCode, "A role with this name already exists", ErrorType.Conflict),
            AuthorizationErrorCodes.RoleInUse => new Error(errorCode, $"Role is assigned to {usersInRole} user(s) and cannot be deleted", ErrorType.Conflict),
            AuthorizationErrorCodes.SystemRoleProtected => new Error(errorCode, "System roles cannot be modified or deleted", ErrorType.Validation),
            AuthorizationErrorCodes.LastAdminProtected => new Error(errorCode, "The system must keep at least one administrator", ErrorType.Validation),
            AuthorizationErrorCodes.UnknownPermission => new Error(errorCode, "One or more permissions do not exist", ErrorType.Validation),
            AuthorizationErrorCodes.AdminRoleImplicit => new Error(errorCode, "Admin role permissions are implicit and cannot be changed", ErrorType.Validation),
            _ => new Error(errorCode, "Unexpected authorization error", ErrorType.Unexpected),
        };
    }
}
