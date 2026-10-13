using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Headbanger.Application.Abstractions.Authorization;
using Headbanger.Domain.Authorization;
using Headbanger.Domain.Constants;
using Headbanger.Infra.Data.Context;
using System.Security.Claims;

namespace Headbanger.Infra.Data.Identity.Authorization
{
    public class PermissionManager : IPermissionManager
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IPermissionChecker _permissionChecker;

        public PermissionManager(
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

        public async Task<IReadOnlyList<string>?> GetRolePermissionsAsync(int roleId, CancellationToken ct = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
                return null;

            return await GetRolePermissionClaimsAsync(role);
        }

        public async Task<PermissionChangeResult> SetRolePermissionsAsync(int roleId, IReadOnlyList<string> permissions, CancellationToken ct = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role is null)
                return PermissionChangeResult.Fail(AuthorizationErrorCodes.RoleNotFound);

            if (role.Name == Roles.Admin)
                return PermissionChangeResult.Fail(AuthorizationErrorCodes.AdminRoleImplicit);

            var desired = Normalize(permissions);
            if (desired is null)
                return PermissionChangeResult.Fail(AuthorizationErrorCodes.UnknownPermission);

            var current = await GetRolePermissionClaimsAsync(role);

            foreach (var permission in current.Except(desired))
                await _roleManager.RemoveClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));

            foreach (var permission in desired.Except(current))
                await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));

            await _permissionChecker.InvalidateRoleAsync(role.Id, ct);

            return PermissionChangeResult.Ok(current, desired);
        }

        public async Task<IReadOnlyList<string>?> GetUserAdditionalPermissionsAsync(int userId, CancellationToken ct = default)
        {
            var user = await FindByUserIdAsync(userId);
            if (user is null)
                return null;

            return await GetUserPermissionClaimsAsync(user);
        }

        public async Task<PermissionChangeResult> SetUserAdditionalPermissionsAsync(int userId, IReadOnlyList<string> permissions, CancellationToken ct = default)
        {
            var user = await FindByUserIdAsync(userId);
            if (user is null)
                return PermissionChangeResult.Fail(AuthorizationErrorCodes.UserNotFound);

            var desired = Normalize(permissions);
            if (desired is null)
                return PermissionChangeResult.Fail(AuthorizationErrorCodes.UnknownPermission);

            var inheritedFromRoles = await GetInheritedPermissionsAsync(user);
            desired = desired.Except(inheritedFromRoles).OrderBy(p => p, StringComparer.Ordinal).ToList();

            var current = await GetUserPermissionClaimsAsync(user);

            foreach (var permission in current.Except(desired))
                await _userManager.RemoveClaimAsync(user, new Claim(CustomClaimTypes.Permission, permission));

            foreach (var permission in desired.Except(current))
                await _userManager.AddClaimAsync(user, new Claim(CustomClaimTypes.Permission, permission));

            await _permissionChecker.InvalidateUserAsync(userId, ct);

            return PermissionChangeResult.Ok(current, desired);
        }

        private async Task<ApplicationUser?> FindByUserIdAsync(int userId) =>
            await _context.Set<ApplicationUser>().FirstOrDefaultAsync(au => au.UserId == userId);

        private async Task<IReadOnlyList<string>> GetRolePermissionClaimsAsync(ApplicationRole role)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            return claims
                .Where(c => c.Type == CustomClaimTypes.Permission)
                .Select(c => c.Value)
                .Distinct()
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
        }

        private async Task<IReadOnlyList<string>> GetUserPermissionClaimsAsync(ApplicationUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            return claims
                .Where(c => c.Type == CustomClaimTypes.Permission)
                .Select(c => c.Value)
                .Distinct()
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
        }

        private async Task<IReadOnlyList<string>> GetInheritedPermissionsAsync(ApplicationUser user)
        {
            var inherited = new HashSet<string>(StringComparer.Ordinal);
            var roleNames = await _userManager.GetRolesAsync(user);

            foreach (var roleName in roleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null)
                    continue;

                foreach (var permission in await GetRolePermissionClaimsAsync(role))
                    inherited.Add(permission);
            }

            return inherited.ToList();
        }

        private static IReadOnlyList<string>? Normalize(IReadOnlyList<string> permissions)
        {
            var normalized = permissions.Distinct(StringComparer.Ordinal).ToList();
            if (normalized.Any(p => !PermissionCatalog.Exists(p)))
                return null;

            return normalized.OrderBy(p => p, StringComparer.Ordinal).ToList();
        }
    }
}
