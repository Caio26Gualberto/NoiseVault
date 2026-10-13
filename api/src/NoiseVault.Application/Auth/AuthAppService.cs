using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Auth.Dtos;
using NoiseVault.Application.Auth.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Interfaces.Authenticate;

namespace NoiseVault.Application.Auth
{
    public class AuthAppService : IAuthAppService
    {
        private readonly IAuthenticateService _authService;
        private readonly IRepository<User> _userRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IApplicationLogger _logger;

        public AuthAppService(
            IAuthenticateService authenticateService,
            IRepository<User> userRepository,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IApplicationLoggerFactory loggerFactory)
        {
            _authService = authenticateService;
            _userRepository = userRepository;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = loggerFactory.Create(LogContext.Auth);
        }

        public async Task<Result<LoginResponseDto>> Login(LoginInputDto input)
        {
            _logger.SetOperation("Login");

            var user = await FindByEmailAsync(input.Email);

            if (user == null)
                return Result<LoginResponseDto>.Fail(InvalidCredentials());

            var (isAuthenticated, isNeededChangePassword) = await _authService.Authenticate(user.Email, input.Password);

            if (!isAuthenticated)
                return Result<LoginResponseDto>.Fail(InvalidCredentials());

            if (!await _authService.IsEmailConfirmed(user.Email))
                return Result<LoginResponseDto>.Fail(
                    new Error("EMAIL_NOT_CONFIRMED", "Confirm your email before signing in", ErrorType.Forbidden));

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

        public async Task<Result<RegistrationPendingDto>> Register(RegisterInputDto input)
        {
            _logger.SetOperation("Register");

            var name = UserNameRules.Normalize(input.Name);

            if (!UserNameRules.IsValid(name))
                return Result<RegistrationPendingDto>.Fail(InvalidName());

            var nickname = NicknameRules.Normalize(input.Nickname);

            if (!NicknameRules.IsValid(nickname))
                return Result<RegistrationPendingDto>.Fail(new Error(
                    "INVALID_NICKNAME",
                    $"Nickname must have {NicknameRules.MinLength} to {NicknameRules.MaxLength} lowercase letters, numbers, dots or underscores",
                    ErrorType.Validation));

            if (NicknameRules.IsReserved(nickname) || await IsNicknameTakenAsync(nickname))
                return Result<RegistrationPendingDto>.Fail(NicknameTaken());

            var email = NormalizeEmail(input.Email);

            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return Result<RegistrationPendingDto>.Fail(InvalidEmail());

            UserCreationResult creation;
            string? confirmationToken;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                creation = await _authService.RegisterUser(email, input.Password, name, nickname);

                if (!creation.Succeeded)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result<RegistrationPendingDto>.Fail(ToRegistrationError(creation));
                }

                confirmationToken = await _authService.GenerateEmailConfirmationTokenAsync(creation.UserId);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("USER_REGISTERED", "User registered, pending email confirmation", userId: creation.UserId);

            await SendConfirmationAsync(creation.UserId, creation.Email, name, confirmationToken);

            return Result<RegistrationPendingDto>.Ok(
                new RegistrationPendingDto { UserId = creation.UserId, Email = creation.Email },
                "REGISTRATION_PENDING_CONFIRMATION",
                "Check your inbox to confirm your email");
        }

        public async Task<Result<ConfirmEmailResultDto>> ConfirmEmail(ConfirmEmailInputDto input)
        {
            _logger.SetOperation("ConfirmEmail");

            var outcome = await _authService.ConfirmEmailAsync(input.UserId, input.Token ?? string.Empty);

            if (outcome == EEmailConfirmationResult.Invalid)
                return Result<ConfirmEmailResultDto>.Fail(
                    new Error("EMAIL_CONFIRMATION_INVALID", "This confirmation link is invalid or has expired", ErrorType.Validation));

            if (outcome == EEmailConfirmationResult.Confirmed)
                _logger.Info("EMAIL_CONFIRMED", "Email confirmed", userId: input.UserId);

            return Result<ConfirmEmailResultDto>.Ok(
                new ConfirmEmailResultDto { AlreadyConfirmed = outcome == EEmailConfirmationResult.AlreadyConfirmed },
                "EMAIL_CONFIRMED",
                "Email confirmed successfully");
        }

        public async Task<Result> ResendConfirmation(ResendConfirmationInputDto input)
        {
            _logger.SetOperation("ResendConfirmation");

            var user = await FindByEmailAsync(input.Email);

            if (user != null && user.IsActive && !await _authService.IsEmailConfirmed(user.Email))
            {
                var token = await _authService.GenerateEmailConfirmationTokenAsync(user.Id);
                await SendConfirmationAsync(user.Id, user.Email, user.Name, token);
            }

            return Result.Ok("CONFIRMATION_EMAIL_SENT", "If the email is registered and pending, a new confirmation was sent");
        }

        public async Task<Result> ForgotPassword(ForgotPasswordInputDto input)
        {
            _logger.SetOperation("ForgotPassword");

            var user = await FindByEmailAsync(input.Email);

            if (user != null && user.IsActive)
            {
                var token = await _authService.GeneratePasswordResetTokenAsync(user.Email);

                if (token != null)
                    await SendPasswordResetAsync(user, token);
            }

            return Result.Ok("PASSWORD_RESET_EMAIL_SENT", "If the email is registered, a reset link was sent");
        }

        public async Task<Result> ResetPassword(ResetPasswordInputDto input)
        {
            _logger.SetOperation("ResetPassword");

            var email = NormalizeEmail(input.Email);
            var outcome = await _authService.ResetPasswordAsync(email, input.Token ?? string.Empty, input.NewPassword ?? string.Empty);

            switch (outcome)
            {
                case EPasswordResetResult.InvalidToken:
                    return Result.Fail(new Error("PASSWORD_RESET_INVALID", "This reset link is invalid or has expired", ErrorType.Validation));

                case EPasswordResetResult.WeakPassword:
                    return Result.Fail(PasswordTooWeak());

                default:
                    var user = await FindByEmailAsync(email);
                    _logger.Info("PASSWORD_RESET", "Password reset through email link", userId: user?.Id);
                    return Result.Ok("PASSWORD_RESET_SUCCESS", "Password reset successfully. Sign in with your new password");
            }
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
            _logger.SetOperation("ChangePassword");

            var email = _currentUser.Email;
            var user = await FindByEmailAsync(email);

            if (user == null)
                return Result<TokensDto>.Fail(InvalidCredentials());

            var outcome = await _authService.ChangePassword(email, input.CurrentPassword, input.NewPassword);

            return outcome switch
            {
                EPasswordChangeResult.InvalidCurrentPassword or EPasswordChangeResult.NotFound =>
                    Result<TokensDto>.Fail(InvalidCredentials()),

                EPasswordChangeResult.WeakPassword =>
                    Result<TokensDto>.Fail(PasswordTooWeak()),

                _ => Result<TokensDto>.Ok(await IssueTokensAsync(user), "PASSWORD_CHANGED", "Password changed successfully"),
            };
        }

        public async Task<Result<TokensDto>> SetInitialPassword(SetInitialPasswordInputDto input)
        {
            _logger.SetOperation("SetInitialPassword");

            var email = _currentUser.Email;
            var user = await FindByEmailAsync(email);

            if (user == null)
                return Result<TokensDto>.Fail(InvalidCredentials());

            if (!await _authService.IsPasswordChangeRequired(email))
                return Result<TokensDto>.Fail(
                    new Error("PASSWORD_CHANGE_NOT_PENDING", "This account has no pending password change", ErrorType.Validation));

            var outcome = await _authService.SetInitialPassword(email, input.NewPassword);

            return outcome switch
            {
                EPasswordChangeResult.WeakPassword => Result<TokensDto>.Fail(PasswordTooWeak()),
                EPasswordChangeResult.Success => Result<TokensDto>.Ok(await IssueTokensAsync(user), "PASSWORD_CHANGED", "Password changed successfully"),
                _ => Result<TokensDto>.Fail(new Error("PASSWORD_CHANGE_FAILED", "Failed to change password", ErrorType.Validation)),
            };
        }

        private async Task SendConfirmationAsync(int userId, string email, string name, string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.Warning("EMAIL_CONFIRMATION_SEND_FAILED", "Confirmation token could not be generated", userId: userId);
                return;
            }

            var sent = false;

            try
            {
                sent = await _emailService.SendEmailConfirmationAsync(new EmailConfirmationEmailDto
                {
                    To = email,
                    Name = name,
                    UserId = userId,
                    Token = token,
                });
            }
            catch (Exception ex)
            {
                _logger.Error("EMAIL_CONFIRMATION_SEND_FAILED", "Confirmation email threw", exception: ex, userId: userId);
                return;
            }

            if (!sent)
                _logger.Warning("EMAIL_CONFIRMATION_SEND_FAILED", "Confirmation email was not accepted by the transport", userId: userId);
        }

        private async Task SendPasswordResetAsync(User user, string token)
        {
            var sent = false;

            try
            {
                sent = await _emailService.SendPasswordResetAsync(new PasswordResetEmailDto
                {
                    To = user.Email,
                    Name = user.Name,
                    Token = token,
                });
            }
            catch (Exception ex)
            {
                _logger.Error("PASSWORD_RESET_SEND_FAILED", "Password reset email threw", exception: ex, userId: user.Id);
                return;
            }

            if (!sent)
                _logger.Warning("PASSWORD_RESET_SEND_FAILED", "Password reset email was not accepted by the transport", userId: user.Id);
        }

        private async Task<TokensDto> IssueTokensAsync(User user) => new()
        {
            Token = await _authService.GenerateJwtToken(user),
            RefreshToken = await _authService.IssueRefreshTokenAsync(user.Email)
        };

        private async Task<bool> IsNicknameTakenAsync(string nickname)
        {
            var existing = await _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Nickname == nickname,
                IncludeDeleted = true,
                AsNoTracking = true,
            });

            return existing != null;
        }

        private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

        private static Error InvalidCredentials() =>
            new("INVALID_CREDENTIALS", "Invalid email or password", ErrorType.Unauthorized);

        private static Error InvalidToken() =>
            new("INVALID_TOKEN", "Invalid or expired token", ErrorType.Unauthorized);

        private static Error InvalidName() =>
            new("INVALID_NAME", $"Name must be between {UserNameRules.MinLength} and {UserNameRules.MaxLength} characters", ErrorType.Validation);

        private static Error InvalidEmail() =>
            new("INVALID_EMAIL", "Enter a valid email address", ErrorType.Validation);

        private static Error NicknameTaken() =>
            new("NICKNAME_TAKEN", "This nickname is already taken", ErrorType.Conflict);

        private static Error PasswordTooWeak() =>
            new("PASSWORD_TOO_WEAK", "Password must be at least 6 characters long and contain at least one number", ErrorType.Validation);

        private static Error ToRegistrationError(UserCreationResult creation) => creation.Failure switch
        {
            EUserCreationFailure.EmailAlreadyInUse =>
                new Error("EMAIL_ALREADY_IN_USE", "This email is already registered", ErrorType.Conflict),

            EUserCreationFailure.WeakPassword => PasswordTooWeak(),

            EUserCreationFailure.InvalidEmail => InvalidEmail(),

            _ => new Error("REGISTRATION_FAILED", "Failed to register user", ErrorType.Validation),
        };

        private async Task<User?> FindByEmailAsync(string? email)
        {
            var normalized = NormalizeEmail(email);

            if (normalized.Length == 0)
                return null;

            return await _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Email == normalized
            });
        }
    }
}
