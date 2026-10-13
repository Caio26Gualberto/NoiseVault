using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Headbanger.Domain.Constants;
using Headbanger.Domain.Entities;
using Headbanger.Domain.Interfaces.Authenticate;
using Headbanger.Infra.Data.Context;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Headbanger.Infra.Data.Identity.AuthenticateService
{
    public class AuthenticateService : IAuthenticateService
    {
        private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public AuthenticateService(UserManager<ApplicationUser> userManager, IConfiguration configuration, AppDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
        }

        public async Task<(bool, bool)> Authenticate(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return (false, false);

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);

            return (passwordValid, user.IsNeededChangePassword);
        }

        public Task<UserCreationResult> RegisterUser(string email, string password, string name) =>
            CreateAsync(email, password, name, Roles.User, passwordChangeRequired: false);

        public Task<UserCreationResult> CreateUser(string email, string temporaryPassword, string name, string role) =>
            CreateAsync(email, temporaryPassword, name, role, passwordChangeRequired: true);

        private async Task<UserCreationResult> CreateAsync(string email, string password, string name, string role, bool passwordChangeRequired)
        {
            email = email.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return UserCreationResult.Fail(EUserCreationFailure.EmailAlreadyInUse, []);

            var validation = await ValidateCredentialsAsync(email, password);

            if (validation != null)
                return validation;

            var user = new User { Email = email, Name = name };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var applicationUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                UserId = user.Id,
                IsNeededChangePassword = passwordChangeRequired
            };

            var result = await _userManager.CreateAsync(applicationUser, password);

            if (!result.Succeeded)
                return UserCreationResult.Fail(
                    MapFailure(result.Errors),
                    result.Errors.Select(e => e.Description).ToList());

            var roleResult = await _userManager.AddToRoleAsync(applicationUser, role);

            if (!roleResult.Succeeded)
                return UserCreationResult.Fail(
                    EUserCreationFailure.Unknown,
                    roleResult.Errors.Select(e => e.Description).ToList());

            return UserCreationResult.Ok(user.Id, applicationUser.Email ?? string.Empty);
        }

        private async Task<UserCreationResult?> ValidateCredentialsAsync(string email, string password)
        {
            var candidate = new ApplicationUser { UserName = email, Email = email };
            var errors = new List<IdentityError>();

            foreach (var userValidator in _userManager.UserValidators)
            {
                var result = await userValidator.ValidateAsync(_userManager, candidate);

                if (!result.Succeeded)
                    errors.AddRange(result.Errors);
            }

            foreach (var passwordValidator in _userManager.PasswordValidators)
            {
                var result = await passwordValidator.ValidateAsync(_userManager, candidate, password);

                if (!result.Succeeded)
                    errors.AddRange(result.Errors);
            }

            if (errors.Count == 0)
                return null;

            return UserCreationResult.Fail(
                MapFailure(errors),
                errors.Select(e => e.Description).ToList());
        }

        private static EUserCreationFailure MapFailure(IEnumerable<IdentityError> errors)
        {
            var codes = errors.Select(e => e.Code).ToList();

            if (codes.Any(c => c.StartsWith("Duplicate", StringComparison.Ordinal)))
                return EUserCreationFailure.EmailAlreadyInUse;

            if (codes.Any(c => c.StartsWith("Password", StringComparison.Ordinal)))
                return EUserCreationFailure.WeakPassword;

            if (codes.Any(c => c.Contains("Email", StringComparison.Ordinal) || c.Contains("UserName", StringComparison.Ordinal)))
                return EUserCreationFailure.InvalidEmail;

            return EUserCreationFailure.Unknown;
        }

        public async Task Logout(string email)
        {
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null) return;

            var tokens = await _context.RefreshTokens
                .Where(rt => rt.ApplicationUserId == identityUser.Id && rt.RevokedAt == null)
                .ToListAsync();

            if (tokens.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var token in tokens)
                token.RevokedAt = now;

            await _context.SaveChangesAsync();
        }

        public async Task<string> GenerateJwtToken(User user)
        {
            var identityUser = await _userManager.FindByEmailAsync(user.Email);
            if (identityUser == null) return string.Empty;

            var roles = await _userManager.GetRolesAsync(identityUser);

            var secretKey = _configuration["JWT:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException(
                    "JWT:SecretKey não configurado. Defina a variável de ambiente JWT__SecretKey.");

            var key = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(UserClaimTypes.UserId, user.Id.ToString()),
            };

            if (identityUser.IsNeededChangePassword)
                claims.Add(new Claim(UserClaimTypes.PasswordChangeRequired, "true"));

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            claims.Add(new Claim("role", roles.FirstOrDefault() ?? Roles.User));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(AccessTokenLifetime),
                Issuer = _configuration["JWT:Issuer"],
                Audience = JwtAudiences.Api,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<string> IssueRefreshTokenAsync(string email)
        {
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null) return string.Empty;

            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var rawToken = Convert.ToBase64String(randomBytes);

            _context.RefreshTokens.Add(new RefreshToken
            {
                ApplicationUserId = identityUser.Id,
                TokenHash = HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return rawToken;
        }

        public async Task<string?> ConsumeRefreshTokenAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken);

            var token = await _context.RefreshTokens
                .Include(rt => rt.ApplicationUser)
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash
                    && rt.RevokedAt == null
                    && rt.ExpiresAt > DateTime.UtcNow);

            if (token == null) return null;

            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return token.ApplicationUser.Email;
        }

        private static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(bytes);
        }

        public async Task<bool> IsPasswordChangeRequired(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user?.IsNeededChangePassword ?? false;
        }

        public async Task<bool> ChangePassword(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            await _userManager.RemovePasswordAsync(user);

            var result = await _userManager.AddPasswordAsync(user, password);
            if (!result.Succeeded)
                return false;

            user.IsNeededChangePassword = false;
            await _userManager.UpdateAsync(user);

            return true;
        }
    }
}
