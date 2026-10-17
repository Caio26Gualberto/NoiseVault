using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Auth.Dtos;
using NoiseVault.Application.Auth.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Auth;
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
        private readonly IExternalIdentityService _externalIdentity;
        private readonly IApplicationLogger _logger;

        public AuthAppService(
            IAuthenticateService authenticateService,
            IRepository<User> userRepository,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IExternalIdentityService externalIdentity,
            IApplicationLoggerFactory loggerFactory)
        {
            _authService = authenticateService;
            _userRepository = userRepository;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _externalIdentity = externalIdentity;
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
                return Result<LoginResponseDto>.Fail(AccountInactive());

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
                return Result<RegistrationPendingDto>.Fail(InvalidNickname());

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

        public async Task<Result<ExternalLoginResponseDto>> ExternalLogin(ExternalLoginInputDto input)
        {
            _logger.SetOperation("ExternalLogin");

            var (identity, error) = await VerifyExternalAsync(input);

            if (identity == null)
                return Result<ExternalLoginResponseDto>.Fail(error!);

            var linkedUserId = await _authService.FindUserIdByLoginAsync(identity.Provider, identity.ProviderKey);

            if (linkedUserId != null)
                return await SignInExternalAsync(linkedUserId.Value, "LOGIN_SUCCESS", "Login successful");

            var email = TrustedEmail(identity);
            var existing = email == null ? null : await FindByEmailAsync(email);

            if (existing != null)
            {
                if (!existing.IsActive)
                    return Result<ExternalLoginResponseDto>.Fail(AccountInactive());

                if (identity.Provider == EExternalProvider.Google)
                    return await AutoLinkAsync(existing, identity);

                var methods = await _authService.GetSignInMethodsAsync(existing.Id);

                return Result<ExternalLoginResponseDto>.Ok(
                    new ExternalLoginResponseDto
                    {
                        Outcome = EExternalLoginOutcome.LinkRequired,
                        Ticket = _externalIdentity.IssueTicket(identity),
                        Email = existing.Email,
                        HasPassword = methods.HasPassword,
                    },
                    "EXTERNAL_LINK_REQUIRED",
                    "An account with this email already exists. Confirm its password to connect");
            }

            return Result<ExternalLoginResponseDto>.Ok(
                new ExternalLoginResponseDto
                {
                    Outcome = EExternalLoginOutcome.SignupRequired,
                    Ticket = _externalIdentity.IssueTicket(identity),
                    Name = UserNameRules.Normalize(identity.Name),
                    Email = email,
                    SuggestedNickname = await SuggestNicknameAsync(identity.Name),
                },
                "EXTERNAL_SIGNUP_REQUIRED",
                "Choose a nickname to finish signing up");
        }

        public async Task<Result<ExternalLoginResponseDto>> ExternalRegister(ExternalRegisterInputDto input)
        {
            _logger.SetOperation("ExternalRegister");

            var identity = _externalIdentity.ReadTicket(input.Ticket);

            if (identity == null)
                return Result<ExternalLoginResponseDto>.Fail(TicketInvalid());

            var linkedUserId = await _authService.FindUserIdByLoginAsync(identity.Provider, identity.ProviderKey);

            if (linkedUserId != null)
                return await SignInExternalAsync(linkedUserId.Value, "LOGIN_SUCCESS", "Login successful");

            var name = UserNameRules.Normalize(input.Name);

            if (!UserNameRules.IsValid(name))
                return Result<ExternalLoginResponseDto>.Fail(InvalidName());

            var nickname = NicknameRules.Normalize(input.Nickname);

            if (!NicknameRules.IsValid(nickname))
                return Result<ExternalLoginResponseDto>.Fail(InvalidNickname());

            if (NicknameRules.IsReserved(nickname) || await IsNicknameTakenAsync(nickname))
                return Result<ExternalLoginResponseDto>.Fail(NicknameTaken());

            var trustedEmail = TrustedEmail(identity);
            var email = trustedEmail ?? NormalizeEmail(input.Email);
            var emailConfirmed = trustedEmail != null;

            if (email.Length == 0)
                return Result<ExternalLoginResponseDto>.Fail(
                    new Error("EXTERNAL_EMAIL_REQUIRED", "Enter an email to finish signing up", ErrorType.Validation));

            if (!email.Contains('@'))
                return Result<ExternalLoginResponseDto>.Fail(InvalidEmail());

            UserCreationResult creation;
            string? confirmationToken = null;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                creation = await _authService.RegisterExternalUser(email, emailConfirmed, name, nickname, identity.Provider, identity.ProviderKey);

                if (!creation.Succeeded)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result<ExternalLoginResponseDto>.Fail(ToRegistrationError(creation));
                }

                if (!emailConfirmed)
                    confirmationToken = await _authService.GenerateEmailConfirmationTokenAsync(creation.UserId);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info(
                "USER_REGISTERED_EXTERNAL",
                "User registered through an external provider",
                userId: creation.UserId,
                extra: new { Provider = identity.Provider.ToString(), EmailConfirmed = emailConfirmed });

            if (!emailConfirmed)
                await SendConfirmationAsync(creation.UserId, creation.Email, name, confirmationToken);

            return emailConfirmed
                ? await SignInExternalAsync(creation.UserId, "EXTERNAL_ACCOUNT_CREATED", "Account created")
                : await SignInExternalAsync(creation.UserId, "EXTERNAL_ACCOUNT_CREATED_CONFIRM_EMAIL", "Account created. Check your inbox to confirm your email");
        }

        public async Task<Result<ExternalLoginResponseDto>> ExternalLink(ExternalLinkInputDto input)
        {
            _logger.SetOperation("ExternalLink");

            var identity = _externalIdentity.ReadTicket(input.Ticket);
            var email = identity == null ? null : TrustedEmail(identity);

            if (identity == null || email == null)
                return Result<ExternalLoginResponseDto>.Fail(TicketInvalid());

            var linkedUserId = await _authService.FindUserIdByLoginAsync(identity.Provider, identity.ProviderKey);

            if (linkedUserId != null)
                return await SignInExternalAsync(linkedUserId.Value, "LOGIN_SUCCESS", "Login successful");

            var user = await FindByEmailAsync(email);

            if (user == null)
                return Result<ExternalLoginResponseDto>.Fail(TicketInvalid());

            var (isAuthenticated, _) = await _authService.Authenticate(user.Email, input.Password ?? string.Empty);

            if (!isAuthenticated)
                return Result<ExternalLoginResponseDto>.Fail(InvalidCredentials());

            if (!user.IsActive)
                return Result<ExternalLoginResponseDto>.Fail(AccountInactive());

            var wasConfirmed = await _authService.IsEmailConfirmed(user.Email);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (!await _authService.AddLoginAsync(user.Id, identity.Provider, identity.ProviderKey))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result<ExternalLoginResponseDto>.Fail(ExternalLoginAlreadyLinked());
                }

                if (!wasConfirmed)
                    await _authService.MarkEmailConfirmedAsync(user.Id);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            LogLinked(user.Id, identity.Provider, "password", claimedUnconfirmed: false);

            await SendLinkedNoticeAsync(user, identity.Provider);

            return await SignInExternalAsync(user.Id, "LOGIN_SUCCESS", "Login successful");
        }

        public async Task<Result<SignInMethodsDto>> GetSignInMethods()
        {
            var methods = await _authService.GetSignInMethodsAsync(_currentUser.UserId);
            return Result<SignInMethodsDto>.Ok(ToDto(methods), "SIGN_IN_METHODS_FOUND", "Sign-in methods found");
        }

        public async Task<Result<SignInMethodsDto>> LinkSignInMethod(ExternalLoginInputDto input)
        {
            _logger.SetOperation("LinkSignInMethod");

            var (identity, error) = await VerifyExternalAsync(input);

            if (identity == null)
                return Result<SignInMethodsDto>.Fail(error!);

            var userId = _currentUser.UserId;
            var linkedUserId = await _authService.FindUserIdByLoginAsync(identity.Provider, identity.ProviderKey);

            if (linkedUserId == userId)
                return Result<SignInMethodsDto>.Fail(ExternalLoginAlreadyLinked());

            if (linkedUserId != null)
                return Result<SignInMethodsDto>.Fail(
                    new Error("EXTERNAL_LOGIN_IN_USE", "This account is already connected to another Noise Vault profile", ErrorType.Conflict));

            var methods = await _authService.GetSignInMethodsAsync(userId);

            if (methods.Providers.Contains(identity.Provider) || !await _authService.AddLoginAsync(userId, identity.Provider, identity.ProviderKey))
                return Result<SignInMethodsDto>.Fail(ExternalLoginAlreadyLinked());

            LogLinked(userId, identity.Provider, "account", claimedUnconfirmed: false);

            var user = await _userRepository.FirstOrDefaultAsync(new QueryOptions<User> { Predicate = u => u.Id == userId, AsNoTracking = true });

            if (user != null && methods.EmailConfirmed)
                await SendLinkedNoticeAsync(user, identity.Provider);

            return Result<SignInMethodsDto>.Ok(
                ToDto(await _authService.GetSignInMethodsAsync(userId)),
                "EXTERNAL_LOGIN_LINKED",
                $"{identity.Provider} connected");
        }

        public async Task<Result<SignInMethodsDto>> UnlinkSignInMethod(int provider)
        {
            _logger.SetOperation("UnlinkSignInMethod");

            if (!Enum.IsDefined(typeof(EExternalProvider), provider))
                return Result<SignInMethodsDto>.Fail(ProviderInvalid());

            var externalProvider = (EExternalProvider)provider;
            var userId = _currentUser.UserId;
            var methods = await _authService.GetSignInMethodsAsync(userId);

            if (!methods.Providers.Contains(externalProvider))
                return Result<SignInMethodsDto>.Fail(
                    new Error("EXTERNAL_LOGIN_NOT_FOUND", "This sign-in method is not connected", ErrorType.NotFound));

            if (!methods.HasPassword && methods.Providers.Count == 1)
                return Result<SignInMethodsDto>.Fail(
                    new Error("LAST_SIGN_IN_METHOD", "Create a password or connect another account before disconnecting this one", ErrorType.Validation));

            await _authService.RemoveLoginAsync(userId, externalProvider);

            _logger.Info("EXTERNAL_LOGIN_UNLINKED", "External login disconnected", userId: userId, extra: new { Provider = externalProvider.ToString() });

            return Result<SignInMethodsDto>.Ok(
                ToDto(await _authService.GetSignInMethodsAsync(userId)),
                "EXTERNAL_LOGIN_UNLINKED",
                $"{externalProvider} disconnected");
        }

        public async Task<Result<SignInMethodsDto>> CreatePassword(CreatePasswordInputDto input)
        {
            _logger.SetOperation("CreatePassword");

            var userId = _currentUser.UserId;
            var outcome = await _authService.CreatePasswordAsync(userId, input.NewPassword ?? string.Empty);

            switch (outcome)
            {
                case EPasswordChangeResult.AlreadySet:
                    return Result<SignInMethodsDto>.Fail(
                        new Error("PASSWORD_ALREADY_SET", "This account already has a password. Use change password instead", ErrorType.Validation));

                case EPasswordChangeResult.WeakPassword:
                    return Result<SignInMethodsDto>.Fail(PasswordTooWeak());

                case EPasswordChangeResult.Success:
                    _logger.Info("PASSWORD_CREATED", "Password created for an account without one", userId: userId);
                    return Result<SignInMethodsDto>.Ok(
                        ToDto(await _authService.GetSignInMethodsAsync(userId)),
                        "PASSWORD_CREATED",
                        "Password created");

                default:
                    return Result<SignInMethodsDto>.Fail(new Error("PASSWORD_CHANGE_FAILED", "Failed to change password", ErrorType.Validation));
            }
        }

        private async Task<(ExternalIdentity? Identity, Error? Error)> VerifyExternalAsync(ExternalLoginInputDto input)
        {
            if (!Enum.IsDefined(typeof(EExternalProvider), input.Provider))
                return (null, ProviderInvalid());

            var provider = (EExternalProvider)input.Provider;
            _logger.Set("Provider", provider.ToString());

            if (string.IsNullOrWhiteSpace(input.Token))
                return (null, ExternalTokenInvalid());

            var verification = await _externalIdentity.VerifyAsync(provider, input.Token);

            return verification.Status switch
            {
                EExternalVerificationStatus.Valid => (verification.Identity, null),
                EExternalVerificationStatus.Disabled => (null, new Error("EXTERNAL_PROVIDER_DISABLED", $"Sign in with {provider} is not available", ErrorType.Validation)),
                EExternalVerificationStatus.Unavailable => (null, new Error("EXTERNAL_PROVIDER_UNAVAILABLE", $"{provider} did not respond. Try again", ErrorType.Unexpected)),
                _ => (null, ExternalTokenInvalid()),
            };
        }

        private async Task<Result<ExternalLoginResponseDto>> AutoLinkAsync(User user, ExternalIdentity identity)
        {
            var wasConfirmed = await _authService.IsEmailConfirmed(user.Email);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (!await _authService.AddLoginAsync(user.Id, identity.Provider, identity.ProviderKey))
                {
                    await _unitOfWork.RollbackAsync();
                    return Result<ExternalLoginResponseDto>.Fail(ExternalLoginAlreadyLinked());
                }

                if (!wasConfirmed)
                    await _authService.ClaimUnconfirmedAccountAsync(user.Id);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            LogLinked(user.Id, identity.Provider, "auto", claimedUnconfirmed: !wasConfirmed);

            if (wasConfirmed)
                await SendLinkedNoticeAsync(user, identity.Provider);

            return await SignInExternalAsync(user.Id, "LOGIN_SUCCESS", "Login successful");
        }

        private async Task<Result<ExternalLoginResponseDto>> SignInExternalAsync(int userId, string code, string message)
        {
            var user = await _userRepository.FirstOrDefaultAsync(new QueryOptions<User> { Predicate = u => u.Id == userId });

            if (user == null)
                return Result<ExternalLoginResponseDto>.Fail(ExternalTokenInvalid());

            if (!user.IsActive)
                return Result<ExternalLoginResponseDto>.Fail(AccountInactive());

            return Result<ExternalLoginResponseDto>.Ok(
                new ExternalLoginResponseDto { Outcome = EExternalLoginOutcome.SignedIn, Tokens = await IssueTokensAsync(user) },
                code,
                message);
        }

        private async Task<string> SuggestNicknameAsync(string name)
        {
            var baseNickname = NicknameRules.DeriveFromName(name);
            var candidate = baseNickname;

            for (var suffix = 2; suffix < 100 && (NicknameRules.IsReserved(candidate) || await IsNicknameTakenAsync(candidate)); suffix++)
                candidate = NicknameRules.WithSuffix(baseNickname, suffix);

            return candidate;
        }

        private void LogLinked(int userId, EExternalProvider provider, string via, bool claimedUnconfirmed) =>
            _logger.Info(
                "EXTERNAL_LOGIN_LINKED",
                "External login connected to an existing account",
                userId: userId,
                extra: new { Provider = provider.ToString(), Via = via, ClaimedUnconfirmed = claimedUnconfirmed });

        private async Task SendLinkedNoticeAsync(User user, EExternalProvider provider)
        {
            var sent = false;

            try
            {
                sent = await _emailService.SendExternalLoginLinkedAsync(new ExternalLoginLinkedEmailDto
                {
                    To = user.Email,
                    Name = user.Name,
                    ProviderName = provider.ToString(),
                });
            }
            catch (Exception ex)
            {
                _logger.Error("EXTERNAL_LOGIN_NOTICE_SEND_FAILED", "External login notice threw", exception: ex, userId: user.Id);
                return;
            }

            if (!sent)
                _logger.Warning("EXTERNAL_LOGIN_NOTICE_SEND_FAILED", "External login notice was not accepted by the transport", userId: user.Id);
        }

        private static string? TrustedEmail(ExternalIdentity identity)
        {
            if (!identity.EmailVerified)
                return null;

            var email = NormalizeEmail(identity.Email);
            return email.Contains('@') ? email : null;
        }

        private static SignInMethodsDto ToDto(SignInMethods methods) => new()
        {
            HasPassword = methods.HasPassword,
            EmailConfirmed = methods.EmailConfirmed,
            Providers = methods.Providers.Select(p => (int)p).Order().ToList(),
        };

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

        private static Error InvalidNickname() =>
            new(
                "INVALID_NICKNAME",
                $"Nickname must have {NicknameRules.MinLength} to {NicknameRules.MaxLength} lowercase letters, numbers, dots or underscores",
                ErrorType.Validation);

        private static Error AccountInactive() =>
            new("ACCOUNT_INACTIVE", "Account is inactive", ErrorType.Forbidden);

        private static Error ProviderInvalid() =>
            new("EXTERNAL_PROVIDER_INVALID", "Unknown sign-in provider", ErrorType.Validation);

        private static Error ExternalTokenInvalid() =>
            new("EXTERNAL_TOKEN_INVALID", "The sign-in could not be verified. Try again", ErrorType.Validation);

        private static Error TicketInvalid() =>
            new("EXTERNAL_TICKET_INVALID", "This sign-in step expired. Start again", ErrorType.Validation);

        private static Error ExternalLoginAlreadyLinked() =>
            new("EXTERNAL_LOGIN_ALREADY_LINKED", "This sign-in method is already connected to your account", ErrorType.Conflict);

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
