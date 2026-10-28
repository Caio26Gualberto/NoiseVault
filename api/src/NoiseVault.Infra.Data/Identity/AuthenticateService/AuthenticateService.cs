using NoiseVault.Application.Auth;
using System.Buffers.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Auth;
using NoiseVault.Domain.Interfaces.Authenticate;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Identity.AuthenticateService
{
    public class AuthenticateService : IAuthenticateService
    {
        private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RefreshReuseGrace = TimeSpan.FromSeconds(30);

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IApplicationLogger _logger;

        public AuthenticateService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            AppDbContext context,
            IApplicationLoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _logger = loggerFactory.Create(LogContext.Auth);
        }

        public async Task<(bool, bool)> Authenticate(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return (false, false);

            var passwordValid = await _userManager.CheckPasswordAsync(user, password);

            return (passwordValid, user.IsNeededChangePassword);
        }

        public Task<UserCreationResult> RegisterUser(string email, string password, string name, string nickname) =>
            CreateAsync(email, password, name, nickname, Roles.User, passwordChangeRequired: false, emailConfirmed: false, login: null);

        public Task<UserCreationResult> CreateUser(string email, string temporaryPassword, string name, string nickname, string role) =>
            CreateAsync(email, temporaryPassword, name, nickname, role, passwordChangeRequired: true, emailConfirmed: true, login: null);

        public Task<UserCreationResult> RegisterExternalUser(string email, bool emailConfirmed, string name, string nickname, EExternalProvider provider, string providerKey) =>
            CreateAsync(email, password: null, name, nickname, Roles.User, passwordChangeRequired: false, emailConfirmed, ToLoginInfo(provider, providerKey));

        private async Task<UserCreationResult> CreateAsync(
            string email,
            string? password,
            string name,
            string nickname,
            string role,
            bool passwordChangeRequired,
            bool emailConfirmed,
            UserLoginInfo? login)
        {
            email = email.Trim().ToLowerInvariant();

            if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email))
                return UserCreationResult.Fail(EUserCreationFailure.EmailAlreadyInUse, []);

            var validation = await ValidateCredentialsAsync(email, password);

            if (validation != null)
                return validation;

            var user = new User { Email = email, Name = name, Nickname = nickname };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var applicationUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = emailConfirmed,
                UserId = user.Id,
                IsNeededChangePassword = passwordChangeRequired
            };

            var result = password == null
                ? await _userManager.CreateAsync(applicationUser)
                : await _userManager.CreateAsync(applicationUser, password);

            if (!result.Succeeded)
                return UserCreationResult.Fail(
                    MapFailure(result.Errors),
                    result.Errors.Select(e => e.Description).ToList());

            var roleResult = await _userManager.AddToRoleAsync(applicationUser, role);

            if (!roleResult.Succeeded)
                return UserCreationResult.Fail(
                    EUserCreationFailure.Unknown,
                    roleResult.Errors.Select(e => e.Description).ToList());

            if (login != null)
            {
                var loginResult = await _userManager.AddLoginAsync(applicationUser, login);

                if (!loginResult.Succeeded)
                    return UserCreationResult.Fail(
                        EUserCreationFailure.Unknown,
                        loginResult.Errors.Select(e => e.Description).ToList());
            }

            return UserCreationResult.Ok(user.Id, applicationUser.Email ?? string.Empty);
        }

        public async Task<int?> FindUserIdByLoginAsync(EExternalProvider provider, string providerKey)
        {
            var user = await _userManager.FindByLoginAsync(provider.ToString(), providerKey);
            return user?.UserId;
        }

        public async Task<bool> AddLoginAsync(int userId, EExternalProvider provider, string providerKey)
        {
            var user = await FindByUserIdAsync(userId);
            if (user == null) return false;

            var logins = await _userManager.GetLoginsAsync(user);

            if (logins.Any(l => l.LoginProvider == provider.ToString()))
                return false;

            var result = await _userManager.AddLoginAsync(user, ToLoginInfo(provider, providerKey));
            return result.Succeeded;
        }

        public async Task<bool> RemoveLoginAsync(int userId, EExternalProvider provider)
        {
            var user = await FindByUserIdAsync(userId);
            if (user == null) return false;

            var login = (await _userManager.GetLoginsAsync(user)).FirstOrDefault(l => l.LoginProvider == provider.ToString());
            if (login == null) return false;

            var result = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
            return result.Succeeded;
        }

        public async Task<SignInMethods> GetSignInMethodsAsync(int userId)
        {
            var user = await FindByUserIdAsync(userId);
            if (user == null) return new SignInMethods(false, false, []);

            var providers = (await _userManager.GetLoginsAsync(user))
                .Select(l => Enum.TryParse<EExternalProvider>(l.LoginProvider, out var provider) ? provider : (EExternalProvider?)null)
                .OfType<EExternalProvider>()
                .Distinct()
                .ToList();

            return new SignInMethods(await _userManager.HasPasswordAsync(user), user.EmailConfirmed, providers);
        }

        public async Task<IReadOnlyDictionary<int, SignInMethods>> GetSignInMethodsAsync(IReadOnlyCollection<int> userIds)
        {
            if (userIds.Count == 0) return new Dictionary<int, SignInMethods>();

            var accounts = await _userManager.Users
                .AsNoTracking()
                .Where(a => userIds.Contains(a.UserId))
                .Select(a => new { a.Id, a.UserId, HasPassword = a.PasswordHash != null, a.EmailConfirmed })
                .ToListAsync();

            var accountIds = accounts.Select(a => a.Id).ToList();
            var logins = await _context.UserLogins
                .AsNoTracking()
                .Where(l => accountIds.Contains(l.UserId))
                .Select(l => new { l.UserId, l.LoginProvider })
                .ToListAsync();

            return accounts.ToDictionary(
                a => a.UserId,
                a => new SignInMethods(
                    a.HasPassword,
                    a.EmailConfirmed,
                    logins
                        .Where(l => l.UserId == a.Id)
                        .Select(l => Enum.TryParse<EExternalProvider>(l.LoginProvider, out var provider) ? provider : (EExternalProvider?)null)
                        .OfType<EExternalProvider>()
                        .Distinct()
                        .ToList()));
        }

        public Task<int> CountUnconfirmedAsync() =>
            _userManager.Users.CountAsync(a => !a.EmailConfirmed);

        public async Task ClaimUnconfirmedAccountAsync(int userId)
        {
            var user = await FindByUserIdAsync(userId);
            if (user == null) return;

            if (await _userManager.HasPasswordAsync(user))
                await _userManager.RemovePasswordAsync(user);

            user.EmailConfirmed = true;
            await _userManager.UpdateSecurityStampAsync(user);
            await RevokeAllRefreshTokensAsync(user.Id);
        }

        public async Task MarkEmailConfirmedAsync(int userId)
        {
            var user = await FindByUserIdAsync(userId);
            if (user == null || user.EmailConfirmed) return;

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        public async Task<EPasswordChangeResult> CreatePasswordAsync(int userId, string newPassword)
        {
            var user = await FindByUserIdAsync(userId);
            if (user == null) return EPasswordChangeResult.NotFound;

            if (await _userManager.HasPasswordAsync(user))
                return EPasswordChangeResult.AlreadySet;

            var result = await _userManager.AddPasswordAsync(user, newPassword);

            if (!result.Succeeded)
                return IsPasswordError(result.Errors) ? EPasswordChangeResult.WeakPassword : EPasswordChangeResult.NotFound;

            return EPasswordChangeResult.Success;
        }

        private static UserLoginInfo ToLoginInfo(EExternalProvider provider, string providerKey) =>
            new(provider.ToString(), providerKey, provider.ToString());

        private async Task<UserCreationResult?> ValidateCredentialsAsync(string email, string? password)
        {
            var candidate = new ApplicationUser { UserName = email, Email = email };
            var errors = new List<IdentityError>();

            foreach (var userValidator in _userManager.UserValidators)
            {
                var result = await userValidator.ValidateAsync(_userManager, candidate);

                if (!result.Succeeded)
                    errors.AddRange(result.Errors);
            }

            foreach (var passwordValidator in password == null ? Enumerable.Empty<IPasswordValidator<ApplicationUser>>() : _userManager.PasswordValidators)
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

        private static bool IsPasswordError(IEnumerable<IdentityError> errors) =>
            errors.Any(e => e.Code.StartsWith("Password", StringComparison.Ordinal) && e.Code != nameof(IdentityErrorDescriber.PasswordMismatch));

        public async Task Logout(string email)
        {
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null) return;

            await RevokeAllRefreshTokensAsync(identityUser.Id);
        }

        private Task<int> RevokeAllRefreshTokensAsync(int applicationUserId)
        {
            var now = DateTime.UtcNow;

            return _context.RefreshTokens
                .Where(rt => rt.ApplicationUserId == applicationUserId && rt.RevokedAt == null)
                .ExecuteUpdateAsync(set => set.SetProperty(rt => rt.RevokedAt, now));
        }

        public async Task<bool> IsEmailConfirmed(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user?.EmailConfirmed ?? false;
        }

        public async Task<string?> GenerateEmailConfirmationTokenAsync(int userId)
        {
            var user = await FindByUserIdAsync(userId);
            if (user == null || user.EmailConfirmed) return null;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            return EncodeToken(token);
        }

        public async Task<EEmailConfirmationResult> ConfirmEmailAsync(int userId, string token)
        {
            var user = await FindByUserIdAsync(userId);
            if (user == null) return EEmailConfirmationResult.Invalid;

            if (user.EmailConfirmed)
                return EEmailConfirmationResult.AlreadyConfirmed;

            var decoded = DecodeToken(token);
            if (decoded == null) return EEmailConfirmationResult.Invalid;

            var result = await _userManager.ConfirmEmailAsync(user, decoded);

            return result.Succeeded ? EEmailConfirmationResult.Confirmed : EEmailConfirmationResult.Invalid;
        }

        public async Task<string?> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.EmailConfirmed) return null;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return EncodeToken(token);
        }

        public async Task<EPasswordResetResult> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return EPasswordResetResult.InvalidToken;

            var decoded = DecodeToken(token);
            if (decoded == null) return EPasswordResetResult.InvalidToken;

            var result = await _userManager.ResetPasswordAsync(user, decoded, newPassword);

            if (!result.Succeeded)
                return IsPasswordError(result.Errors) ? EPasswordResetResult.WeakPassword : EPasswordResetResult.InvalidToken;

            await RevokeAllRefreshTokensAsync(user.Id);

            return EPasswordResetResult.Success;
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
                ExpiresAt = DateTime.UtcNow.Add(AuthTokenRules.RefreshTokenLifetime),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return rawToken;
        }

        public async Task<string?> ConsumeRefreshTokenAsync(string refreshToken)
        {
            var hash = HashToken(refreshToken ?? string.Empty);
            var now = DateTime.UtcNow;

            var token = await _context.RefreshTokens
                .AsNoTracking()
                .Include(rt => rt.ApplicationUser)
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (token == null) return null;

            if (token.RevokedAt != null)
            {
                if (token.RevokedAt < now - RefreshReuseGrace)
                {
                    await RevokeAllRefreshTokensAsync(token.ApplicationUserId);

                    _logger.Warning(
                        "REFRESH_TOKEN_REUSE",
                        "Revoked refresh token presented again; all sessions revoked",
                        extra: new { token.ApplicationUserId });
                }

                return null;
            }

            if (token.ExpiresAt <= now) return null;

            var consumed = await _context.RefreshTokens
                .Where(rt => rt.Id == token.Id && rt.RevokedAt == null)
                .ExecuteUpdateAsync(set => set.SetProperty(rt => rt.RevokedAt, now));

            if (consumed == 0) return null;

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

        public async Task<EPasswordChangeResult> ChangePassword(string email, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return EPasswordChangeResult.NotFound;

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
                return IsPasswordError(result.Errors)
                    ? EPasswordChangeResult.WeakPassword
                    : EPasswordChangeResult.InvalidCurrentPassword;

            await ClearPasswordChangeFlagAsync(user);

            return EPasswordChangeResult.Success;
        }

        public async Task<EPasswordChangeResult> SetInitialPassword(string email, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return EPasswordChangeResult.NotFound;

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
                return IsPasswordError(result.Errors) ? EPasswordChangeResult.WeakPassword : EPasswordChangeResult.NotFound;

            await ClearPasswordChangeFlagAsync(user);

            return EPasswordChangeResult.Success;
        }

        private async Task ClearPasswordChangeFlagAsync(ApplicationUser user)
        {
            await RevokeAllRefreshTokensAsync(user.Id);

            if (!user.IsNeededChangePassword)
                return;

            user.IsNeededChangePassword = false;
            await _userManager.UpdateAsync(user);
        }

        private Task<ApplicationUser?> FindByUserIdAsync(int userId) =>
            _userManager.Users.FirstOrDefaultAsync(a => a.UserId == userId);

        private static string EncodeToken(string token) =>
            Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token));

        private static string? DecodeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                return Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
