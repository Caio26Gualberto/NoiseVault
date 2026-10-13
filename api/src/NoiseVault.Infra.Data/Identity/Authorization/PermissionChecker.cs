using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Domain.Authorization;
using NoiseVault.Domain.Constants;
using NoiseVault.Infra.Data.Cache;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Identity.Authorization
{
    public class PermissionChecker : IPermissionChecker
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly ICacheClient _cacheClient;

        public PermissionChecker(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            AppDbContext context,
            ICacheClient cacheClient)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _cacheClient = cacheClient;
        }

        public async Task<bool> IsGrantedAsync(int userId, string permission, CancellationToken ct = default)
        {
            if (!PermissionCatalog.Exists(permission))
                return false;

            var snapshot = await GetUserPermissionsAsync(userId, ct);
            return snapshot is not null && snapshot.EffectivePermissions.Contains(permission);
        }

        public async Task<UserPermissionsSnapshot?> GetUserPermissionsAsync(int userId, CancellationToken ct = default)
        {
            var snapshot = await _cacheClient.GetOrCreateAsync(
                AuthorizationCacheKeys.UserKey(userId),
                async _ => await BuildSnapshotAsync(userId),
                new CacheEntryOptions(CacheTtl, Tags: new[] { AuthorizationCacheKeys.Tag }),
                ct);

            return snapshot;
        }

        public async Task InvalidateUserAsync(int userId, CancellationToken ct = default)
        {
            await _cacheClient.RemoveAsync(AuthorizationCacheKeys.UserKey(userId), ct);
        }

        public async Task InvalidateRoleAsync(int roleId, CancellationToken ct = default)
        {
            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Join(_context.Set<ApplicationUser>(), ur => ur.UserId, au => au.Id, (ur, au) => au.UserId)
                .ToListAsync(ct);

            foreach (var userId in userIds)
                await _cacheClient.RemoveAsync(AuthorizationCacheKeys.UserKey(userId), ct);
        }

        private async Task<UserPermissionsSnapshot?> BuildSnapshotAsync(int userId)
        {
            var applicationUser = await _context.Set<ApplicationUser>()
                .FirstOrDefaultAsync(au => au.UserId == userId);

            if (applicationUser is null)
                return null;

            var roleNames = await _userManager.GetRolesAsync(applicationUser);

            var roles = new List<RoleRef>();
            var rolePermissions = new HashSet<string>(StringComparer.Ordinal);
            var isAdmin = false;

            foreach (var roleName in roleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null)
                    continue;

                roles.Add(new RoleRef(role.Id, role.Name!));

                if (role.Name == Roles.Admin)
                {
                    isAdmin = true;
                    continue;
                }

                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims)
                {
                    if (claim.Type == CustomClaimTypes.Permission && PermissionCatalog.Exists(claim.Value))
                        rolePermissions.Add(claim.Value);
                }
            }

            var userPermissions = new HashSet<string>(StringComparer.Ordinal);
            var userClaims = await _userManager.GetClaimsAsync(applicationUser);
            foreach (var claim in userClaims)
            {
                if (claim.Type == CustomClaimTypes.Permission && PermissionCatalog.Exists(claim.Value))
                    userPermissions.Add(claim.Value);
            }

            IReadOnlyList<string> effective = isAdmin
                ? PermissionCatalog.AllNames
                : rolePermissions.Union(userPermissions).OrderBy(p => p, StringComparer.Ordinal).ToList();

            return new UserPermissionsSnapshot(
                roles,
                rolePermissions.OrderBy(p => p, StringComparer.Ordinal).ToList(),
                userPermissions.OrderBy(p => p, StringComparer.Ordinal).ToList(),
                effective);
        }
    }
}
