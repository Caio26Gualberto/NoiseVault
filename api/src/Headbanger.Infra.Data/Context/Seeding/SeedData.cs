using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Headbanger.Domain.Authorization;
using Headbanger.Domain.Constants;
using Headbanger.Domain.Entities;
using Headbanger.Infra.Data.Cache;
using Headbanger.Infra.Data.Identity;
using Headbanger.Infra.Data.Identity.Authorization;

namespace Headbanger.Infra.Data.Context.Seeding
{
    public class SeedData
    {
        private const string AdminEmail = "admin@headbanger.local";
        private const string AdminName = "Admin";
        private const string AdminNickname = "admin";
        private const string DefaultAdminPassword = "12341234";

        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly ICacheClient _cacheClient;
        private readonly IConfiguration _configuration;

        public SeedData(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            ICacheClient cacheClient,
            IConfiguration configuration)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
            _cacheClient = cacheClient;
            _configuration = configuration;
        }

        private string AdminPassword =>
            _configuration["Seed:AdminPassword"] is { Length: > 0 } configured
                ? configured
                : DefaultAdminPassword;

        public async Task SeedAsync(bool includeAdminAccount)
        {
            await SeedRolesAsync();
            await SeedRolePermissionsAsync(Roles.User, RoleDefaultPermissions.User);

            if (includeAdminAccount)
                await SeedUserAsync(AdminEmail, AdminName, Roles.Admin);

            await _cacheClient.RemoveByTagAsync(AuthorizationCacheKeys.Tag);
        }

        private async Task SeedRolesAsync()
        {
            var roles = new (string Name, string Description, bool IsSystem, bool IsDefault)[]
            {
                (Roles.User, "Usuário final, sem acesso ao painel.", true, true),
                (Roles.Admin, "Acesso completo a todas as funcionalidades.", true, false),
            };

            foreach (var (name, description, isSystem, isDefault) in roles)
            {
                var role = await _roleManager.FindByNameAsync(name);

                if (role is null)
                {
                    await _roleManager.CreateAsync(new ApplicationRole(name)
                    {
                        Description = description,
                        IsSystem = isSystem,
                        IsDefault = isDefault,
                    });
                    continue;
                }

                if (role.IsSystem != isSystem || role.IsDefault != isDefault || string.IsNullOrEmpty(role.Description))
                {
                    role.IsSystem = isSystem;
                    role.IsDefault = isDefault;
                    if (string.IsNullOrEmpty(role.Description))
                        role.Description = description;
                    await _roleManager.UpdateAsync(role);
                }
            }
        }

        private async Task SeedRolePermissionsAsync(string roleName, IReadOnlyList<string> permissions)
        {
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role is null)
                return;

            var claims = await _roleManager.GetClaimsAsync(role);
            var granted = claims.Where(c => c.Type == CustomClaimTypes.Permission).ToList();

            foreach (var stale in granted.Where(c => !PermissionCatalog.Exists(c.Value)))
                await _roleManager.RemoveClaimAsync(role, stale);

            var current = granted.Select(c => c.Value).ToHashSet();

            foreach (var permission in permissions.Distinct().Where(p => !current.Contains(p)))
                await _roleManager.AddClaimAsync(role, new Claim(CustomClaimTypes.Permission, permission));
        }

        private async Task SeedUserAsync(string email, string name, string role)
        {
            var existing = await _userManager.FindByEmailAsync(email);

            if (existing is not null)
            {
                var currentRoles = await _userManager.GetRolesAsync(existing);
                if (currentRoles.Count == 0)
                    await _userManager.AddToRoleAsync(existing, role);

                if (!await _userManager.CheckPasswordAsync(existing, AdminPassword))
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existing);
                    await _userManager.ResetPasswordAsync(existing, resetToken, AdminPassword);
                }

                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new User { Email = email, Name = name, Nickname = AdminNickname };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var applicationUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    UserId = user.Id,
                };

                var result = await _userManager.CreateAsync(applicationUser, AdminPassword);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                await _userManager.AddToRoleAsync(applicationUser, role);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
