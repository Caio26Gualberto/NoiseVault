using Headbanger.Application.Abstractions.Authentication;
using Headbanger.Application.Abstractions.Repository;
using Headbanger.Application.Auth.Dtos;
using Headbanger.Application.Auth.Interfaces;
using Headbanger.Application.Response;
using Headbanger.Domain.Entities;
using Headbanger.Domain.Interfaces.Authenticate;

namespace Headbanger.Application.Auth
{
    public class AuthAppService : IAuthAppService
    {
        private readonly IAuthenticateService _authService;
        private readonly IRepository<User> _userRepository;
        private readonly ICurrentUser _currentUser;

        public AuthAppService(
            IAuthenticateService authenticateService,
            IRepository<User> userRepository,
            ICurrentUser currentUser)
        {
            _authService = authenticateService;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<LoginResponseDto>> Login(LoginInputDto input)
        {
            var user = await FindByEmailAsync(input.Email);

            if (user == null)
                return Result<LoginResponseDto>.Fail(InvalidCredentials());

            var (isAuthenticated, isNeededChangePassword) = await _authService.Authenticate(input.Email, input.Password);

            if (!isAuthenticated)
                return Result<LoginResponseDto>.Fail(InvalidCredentials());

            if (!user.IsActive)
                return Result<LoginResponseDto>.Fail(
                    new Error("ACCOUNT_INACTIVE", "Account is inactive", ErrorType.Forbidden));

            return Result<LoginResponseDto>.Ok(
                new LoginResponseDto
                {
                    Tokens = await IssueTokensAsync(user),
                    IsNeededChangePassword = isNeededChangePassword
                },
                "LOGIN_SUCCESS",
                "Login successful");
        }

        public async Task<Result<LoginResponseDto>> Register(RegisterInputDto input)
        {
            var name = UserNameRules.Normalize(input.Name);

            if (!UserNameRules.IsValid(name))
                return Result<LoginResponseDto>.Fail(new Error(
                    "INVALID_NAME",
                    $"Name must be between {UserNameRules.MinLength} and {UserNameRules.MaxLength} characters",
                    ErrorType.Validation));

            var email = (input.Email ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return Result<LoginResponseDto>.Fail(
                    new Error("INVALID_EMAIL", "Enter a valid email address", ErrorType.Validation));

            var creation = await _authService.RegisterUser(email, input.Password, name);

            if (!creation.Succeeded)
                return Result<LoginResponseDto>.Fail(ToRegistrationError(creation));

            var user = await FindByEmailAsync(creation.Email);

            if (user == null)
                return Result<LoginResponseDto>.Fail(
                    new Error("REGISTRATION_FAILED", "Failed to register user", ErrorType.Unexpected));

            return Result<LoginResponseDto>.Ok(
                new LoginResponseDto { Tokens = await IssueTokensAsync(user), IsNeededChangePassword = false },
                "REGISTRATION_SUCCESS",
                "User registered successfully");
        }

        public async Task<Result<TokensDto>> RefreshTokens(RefreshTokenRequestDto input)
        {
            var email = await _authService.ConsumeRefreshTokenAsync(input.RefreshToken);

            if (string.IsNullOrEmpty(email))
                return Result<TokensDto>.Fail(InvalidToken());

            var user = await FindByEmailAsync(email);

            if (user == null || !user.IsActive)
                return Result<TokensDto>.Fail(InvalidToken());

            return Result<TokensDto>.Ok(await IssueTokensAsync(user), "TOKENS_RENEWED", "Tokens successfully renewed.");
        }

        public async Task<Result<bool>> Logout()
        {
            await _authService.Logout(_currentUser.Email);
            return Result<bool>.Ok(true, "LOGOUT_SUCCESS", "Logged out successfully");
        }

        public async Task<Result<TokensDto>> ChangePassword(ChangePasswordInputDto input)
        {
            var email = _currentUser.Email;
            var user = await FindByEmailAsync(email);

            if (user == null)
                return Result<TokensDto>.Fail(InvalidCredentials());

            var (isAuthenticated, _) = await _authService.Authenticate(email, input.CurrentPassword);

            if (!isAuthenticated)
                return Result<TokensDto>.Fail(InvalidCredentials());

            if (!await _authService.ChangePassword(email, input.NewPassword))
                return Result<TokensDto>.Fail(
                    new Error("PASSWORD_CHANGE_FAILED", "Failed to change password", ErrorType.Validation));

            return Result<TokensDto>.Ok(await IssueTokensAsync(user), "PASSWORD_CHANGED", "Password changed successfully");
        }

        public async Task<Result<TokensDto>> SetInitialPassword(SetInitialPasswordInputDto input)
        {
            var email = _currentUser.Email;
            var user = await FindByEmailAsync(email);

            if (user == null)
                return Result<TokensDto>.Fail(InvalidCredentials());

            if (!await _authService.IsPasswordChangeRequired(email))
                return Result<TokensDto>.Fail(
                    new Error("PASSWORD_CHANGE_NOT_PENDING", "This account has no pending password change", ErrorType.Validation));

            if (!await _authService.ChangePassword(email, input.NewPassword))
                return Result<TokensDto>.Fail(
                    new Error("PASSWORD_CHANGE_FAILED", "Failed to change password", ErrorType.Validation));

            return Result<TokensDto>.Ok(await IssueTokensAsync(user), "PASSWORD_CHANGED", "Password changed successfully");
        }

        private async Task<TokensDto> IssueTokensAsync(User user) => new()
        {
            Token = await _authService.GenerateJwtToken(user),
            RefreshToken = await _authService.IssueRefreshTokenAsync(user.Email)
        };

        private static Error InvalidCredentials() =>
            new("INVALID_CREDENTIALS", "Invalid email or password", ErrorType.Unauthorized);

        private static Error InvalidToken() =>
            new("INVALID_TOKEN", "Invalid or expired token", ErrorType.Unauthorized);

        private static Error ToRegistrationError(UserCreationResult creation) => creation.Failure switch
        {
            EUserCreationFailure.EmailAlreadyInUse =>
                new Error("EMAIL_ALREADY_IN_USE", "This email is already registered", ErrorType.Conflict),

            EUserCreationFailure.WeakPassword =>
                new Error("PASSWORD_TOO_WEAK", "Password must be at least 6 characters long and contain at least one number", ErrorType.Validation),

            EUserCreationFailure.InvalidEmail =>
                new Error("INVALID_EMAIL", "Enter a valid email address", ErrorType.Validation),

            _ => new Error("REGISTRATION_FAILED", "Failed to register user", ErrorType.Validation),
        };

        private async Task<User?> FindByEmailAsync(string email)
        {
            var normalized = (email ?? string.Empty).Trim();

            return await _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Email == normalized
            });
        }
    }
}
