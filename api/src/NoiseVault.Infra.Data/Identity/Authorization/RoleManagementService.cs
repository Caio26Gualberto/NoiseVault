using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Domain.Constants;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Identity.Authorization
{
    public class RoleManagementService : IRoleManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IPermissionChecker _permissionChecker;

        public RoleManagementService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            AppDbContext context,
            IPermissionChecker permissionChecker)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _permissionChecker = permissionChecker;
        }

        public async Task<IReadOnlyList<RoleInfo>> ListRolesAsync(CancellationToken ct = default)
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    Role = r,
                    UserCount = _context.UserRoles.Count(ur => ur.RoleId == r.Id),
                    PermissionCount = _context.RoleClaims.Count(rc => rc.RoleId == r.Id && rc.ClaimType == CustomClaimTypes.Permission),
                })
                .ToListAsync(ct);

            return roles
                .Select(r => ToRoleInfo(r.Role, r.UserCount, r.PermissionCount))
                .ToList();
        }

        public async Task<RoleInfo?> GetRoleAsync(int roleId, CancellationToken ct = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
                return null;

            return ToRoleInfo(role, await CountUsersAsync(role.Id, ct), await CountPermissionsAsync(role.Id, ct));
        }

        public async Task<RoleOpResult> CreateRoleAsync(string name, string? description, CancellationToken ct = default)
        {
            if (await _roleManager.FindByNameAsync(name) is not null)
                return RoleOpResult.Fail(AuthorizationErrorCodes.RoleNameTaken);

            var role = new ApplicationRole(name) { Description = description };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return RoleOpResult.Fail(AuthorizationErrorCodes.RoleNameTaken);

            return RoleOpResult.Ok(ToRoleInfo(role, 0, 0));
        }

        public async Task<RoleOpResult> UpdateRoleAsync(int roleId, string name, string? description, CancellationToken ct = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
                return RoleOpResult.Fail(AuthorizationErrorCodes.RoleNotFound);

            if (role.IsSystem && role.Name != name)
                return RoleOpResult.Fail(AuthorizationErrorCodes.SystemRoleProtected);

            if (role.Name != name && await _roleManager.FindByNameAsync(name) is not null)
                return RoleOpResult.Fail(AuthorizationErrorCodes.RoleNameTaken);

            role.Name = name;
            role.Description = description;
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
                return RoleOpResult.Fail(AuthorizationErrorCodes.RoleNameTaken);

            return RoleOpResult.Ok(ToRoleInfo(role, await CountUsersAsync(role.Id, ct), await CountPermissionsAsync(role.Id, ct)));
        }

        public async Task<RoleOpResult> DeleteRoleAsync(int roleId, CancellationToken ct = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
                return RoleOpResult.Fail(AuthorizationErrorCodes.RoleNotFound);

            if (role.IsSystem)
                return RoleOpResult.Fail(AuthorizationErrorCodes.SystemRoleProtected);

            var userCount = await CountUsersAsync(role.Id, ct);
            if (userCount > 0)
                return RoleOpResult.Fail(AuthorizationErrorCodes.RoleInUse, userCount);

            var snapshot = ToRoleInfo(role, 0, await CountPermissionsAsync(role.Id, ct));
            await _roleManager.DeleteAsync(role);

            return RoleOpResult.Ok(snapshot);
        }

        public async Task<RoleOpResult> AssignUserRoleAsync(int userId, int roleId, CancellationToken ct = default)
        {
            var user = await _context.Set<ApplicationUser>()
                .FirstOrDefaultAsync(au => au.UserId == userId, ct);
            if (user is null)
                return RoleOpResult.Fail(AuthorizationErrorCodes.UserNotFound);

            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
                return RoleOpResult.Fail(AuthorizationErrorCodes.RoleNotFound);

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(Roles.Admin) && role.Name != Roles.Admin && await IsLastAdminAsync(user, ct))
                return RoleOpResult.Fail(AuthorizationErrorCodes.LastAdminProtected);

            if (currentRoles.Count == 1 && currentRoles[0] == role.Name)
                return RoleOpResult.Ok(ToRoleInfo(role, await CountUsersAsync(role.Id, ct), await CountPermissionsAsync(role.Id, ct)));

            if (currentRoles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            await _userManager.AddToRoleAsync(user, role.Name!);

            await _permissionChecker.InvalidateUserAsync(userId, ct);

            return RoleOpResult.Ok(ToRoleInfo(role, await CountUsersAsync(role.Id, ct), await CountPermissionsAsync(role.Id, ct)));
        }

        public async Task<RevokeAccessResult> RevokeUserAccessAsync(int userId, CancellationToken ct = default)
        {
            var user = await _context.Set<ApplicationUser>()
                .FirstOrDefaultAsync(au => au.UserId == userId, ct);
            if (user is null)
                return RevokeAccessResult.Fail(AuthorizationErrorCodes.UserNotFound);

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains(Roles.Admin) && await IsLastAdminAsync(user, ct))
                return RevokeAccessResult.Fail(AuthorizationErrorCodes.LastAdminProtected);

            var claims = await _userManager.GetClaimsAsync(user);
            var permissions = claims
                .Where(c => c.Type == CustomClaimTypes.Permission)
                .Select(c => c.Value)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            var tokens = await _context.RefreshTokens
                .Where(rt => rt.ApplicationUserId == user.Id)
                .ToListAsync(ct);

            if (tokens.Count > 0)
                _context.RefreshTokens.RemoveRange(tokens);

            if (claims.Count > 0)
                await _userManager.RemoveClaimsAsync(user, claims);

            if (roles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, roles);

            await _userManager.DeleteAsync(user);

            await _permissionChecker.InvalidateUserAsync(userId, ct);

            return RevokeAccessResult.Ok(roles.ToList(), permissions);
        }

        public async Task<IReadOnlyList<UserAccessInfo>> ListUsersWithRolesAsync(CancellationToken ct = default)
        {
            var userRoleId = await _roleManager.Roles
                .Where(r => r.Name == Roles.User)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(ct);

            var staff = await _context.UserRoles
                .Where(ur => userRoleId == null || ur.RoleId != userRoleId)
                .Join(_context.Set<ApplicationUser>(), ur => ur.UserId, au => au.Id, (ur, au) => new { ur.RoleId, DomainId = au.UserId })
                .Join(_roleManager.Roles, x => x.RoleId, r => r.Id, (x, r) => new { x.DomainId, RoleName = r.Name! })
                .Join(_context.Users, x => x.DomainId, a => a.Id, (x, a) => new { a.Id, a.Name, a.Email, x.RoleName })
                .OrderBy(s => s.Name)
                .ToListAsync(ct);

            return staff
                .Select(s => new UserAccessInfo(s.Id, s.Name, s.Email, s.RoleName))
                .ToList();
        }

        private async Task<bool> IsLastAdminAsync(ApplicationUser user, CancellationToken ct)
        {
            var adminRoleId = await _roleManager.Roles
                .Where(r => r.Name == Roles.Admin)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(ct);

            if (adminRoleId is null)
                return false;

            var otherAdmins = await _context.UserRoles
                .CountAsync(ur => ur.RoleId == adminRoleId && ur.UserId != user.Id, ct);

            return otherAdmins == 0;
        }

        private async Task<int> CountUsersAsync(int roleId, CancellationToken ct) =>
            await _context.UserRoles.CountAsync(ur => ur.RoleId == roleId, ct);

        private async Task<int> CountPermissionsAsync(int roleId, CancellationToken ct) =>
            await _context.RoleClaims.CountAsync(rc => rc.RoleId == roleId && rc.ClaimType == CustomClaimTypes.Permission, ct);

        private static RoleInfo ToRoleInfo(ApplicationRole role, int userCount, int permissionCount) =>
            new(role.Id, role.Name!, role.Description, role.IsSystem, role.IsDefault, userCount, permissionCount);
    }
}
