using System.Text.Json;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Auth;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Application.Users.Dtos;
using NoiseVault.Application.Users.Interfaces;
using NoiseVault.Domain.Authorization;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Interfaces.Authenticate;

namespace NoiseVault.Application.Users
{
    public class UserAppService : IUserAppService
    {
        private const int MaxEmailLength = 256;

        private readonly IRepository<User> _repository;
        private readonly IRepository<AuthorizationAuditLog> _auditRepository;
        private readonly IAuthenticateService _authService;
        private readonly IEmailService _emailService;
        private readonly IRoleManagementService _roleManagementService;
        private readonly IPermissionManager _permissionManager;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;

        public UserAppService(
            IRepository<User> repository,
            IRepository<AuthorizationAuditLog> auditRepository,
            IAuthenticateService authService,
            IEmailService emailService,
            IRoleManagementService roleManagementService,
            IPermissionManager permissionManager,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork,
            IFileStorage fileStorage)
        {
            _repository = repository;
            _auditRepository = auditRepository;
            _authService = authService;
            _emailService = emailService;
            _roleManagementService = roleManagementService;
            _permissionManager = permissionManager;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
        }

        public async Task<Result<PagedResultDto<UserDto>>> GetAll(UserListQueryDto query)
        {
            query.Normalize();

            var search = query.Search?.Trim().ToLowerInvariant();

            var options = new QueryOptions<User>
            {
                Predicate = u => u.IsActive && (string.IsNullOrEmpty(search) || u.Name.ToLower().Contains(search) || u.Nickname.Contains(search)),
                Includes = [u => u.City!],
                OrderBy = q => q.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _repository.CountAsync(options);
            var users = await _repository.ListAsync(options);
            var methods = await _authService.GetSignInMethodsAsync(users.Select(u => u.Id).ToList());

            var page = PagedResultDto<UserDto>.Create(
                users.Select(u => Map(u, methods.GetValueOrDefault(u.Id))).ToList(),
                query.Page,
                query.PageSize,
                totalCount);

            return Result<PagedResultDto<UserDto>>.Ok(page, "USERS_LOADED", "Users loaded successfully");
        }

        public async Task<Result<UserSignupSummaryDto>> Summary()
        {
            var now = DateTime.UtcNow;
            var todayStart = AppTime.TodayStartUtc;
            var last7Start = now.AddDays(-7);
            var last30Start = now.AddDays(-30);

            var summary = new UserSignupSummaryDto
            {
                Today = await _repository.CountAsync(new QueryOptions<User> { Predicate = u => u.IsActive && u.CreatedAt >= todayStart }),
                Last7Days = await _repository.CountAsync(new QueryOptions<User> { Predicate = u => u.IsActive && u.CreatedAt >= last7Start }),
                Last30Days = await _repository.CountAsync(new QueryOptions<User> { Predicate = u => u.IsActive && u.CreatedAt >= last30Start }),
                Total = await _repository.CountAsync(new QueryOptions<User> { Predicate = u => u.IsActive }),
                Unconfirmed = await _authService.CountUnconfirmedAsync(),
            };

            return Result<UserSignupSummaryDto>.Ok(summary, "USER_SIGNUP_SUMMARY_FOUND", "Signup summary found");
        }

        public async Task<Result<UserDto>> Get(int userId)
        {
            var user = await _repository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Id == userId,
                Includes = [u => u.City!],
                AsNoTracking = true,
            });

            if (user == null)
                return Result<UserDto>.Fail(new Error("USER_NOT_FOUND", "User not found", ErrorType.NotFound));

            var methods = await _authService.GetSignInMethodsAsync(user.Id);

            return Result<UserDto>.Ok(Map(user, methods), "USER_FOUND", "User found successfully");
        }

        public async Task<Result<InvitedUserDto>> Invite(InviteUserInputDto input)
        {
            var name = UserNameRules.Normalize(input.Name);
            var email = (input.Email ?? string.Empty).Trim().ToLowerInvariant();

            if (!UserNameRules.IsValid(name))
                return Result<InvitedUserDto>.Fail(
                    new Error("INVALID_NAME", $"Name must be between {UserNameRules.MinLength} and {UserNameRules.MaxLength} characters", ErrorType.Validation));

            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return Result<InvitedUserDto>.Fail(
                    new Error("INVALID_EMAIL", "Email is invalid", ErrorType.Validation));

            var role = await _roleManagementService.GetRoleAsync(input.RoleId);

            if (role is null)
                return Result<InvitedUserDto>.Fail(
                    new Error("ROLE_NOT_FOUND", "Role not found", ErrorType.NotFound));

            var requestedPermissions = role.Name == Roles.Admin
                ? []
                : input.AdditionalPermissions ?? [];

            var additionalPermissions = requestedPermissions
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (additionalPermissions.Any(p => !PermissionCatalog.Exists(p)))
                return Result<InvitedUserDto>.Fail(
                    new Error("INVALID_PERMISSION", "One or more permissions do not exist", ErrorType.Validation));

            var existing = await _repository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Email == email,
                AsNoTracking = true,
            });

            if (existing != null)
                return Result<InvitedUserDto>.Fail(
                    new Error("EMAIL_ALREADY_IN_USE", "Email is already in use", ErrorType.Conflict));

            var temporaryPassword = TemporaryPasswordGenerator.Generate();
            var nickname = await ResolveNicknameAsync(email);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var creation = await _authService.CreateUser(email, temporaryPassword, name, nickname, role.Name);

                if (!creation.Succeeded)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result<InvitedUserDto>.Fail(
                        new Error("USER_CREATION_FAILED", string.Join(" ", creation.Errors), ErrorType.Validation));
                }

                var grantedPermissions = new List<string>();

                if (additionalPermissions.Count > 0)
                {
                    var permissionResult = await _permissionManager.SetUserAdditionalPermissionsAsync(creation.UserId, additionalPermissions);

                    if (!permissionResult.Succeeded)
                    {
                        await _unitOfWork.RollbackAsync();
                        return Result<InvitedUserDto>.Fail(
                            new Error("INVALID_PERMISSION", "One or more permissions do not exist", ErrorType.Validation));
                    }

                    grantedPermissions = permissionResult.After!.ToList();
                }

                await _auditRepository.AddAsync(new AuthorizationAuditLog
                {
                    Action = AuthorizationAuditActions.UserInvited,
                    ActorUserId = _currentUser.UserId,
                    ActorEmail = _currentUser.Email,
                    TargetType = "User",
                    TargetId = creation.UserId.ToString(),
                    TargetName = name,
                    NewValueJson = JsonSerializer.Serialize(new { Email = email, Role = role.Name, Permissions = grantedPermissions }),
                    IpAddress = _currentUser.IpAddress,
                });

                var emailSent = await _emailService.SendUserInviteEmail(email, name, role.Name, temporaryPassword);

                if (!emailSent)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result<InvitedUserDto>.Fail(
                        new Error("INVITE_EMAIL_FAILED", "Failed to send the invite email. Please try again later.", ErrorType.Unexpected));
                }

                await _unitOfWork.CommitAsync();

                return Result<InvitedUserDto>.Ok(
                    new InvitedUserDto
                    {
                        Id = creation.UserId,
                        Name = name,
                        Email = creation.Email,
                        Role = role.Name,
                        AdditionalPermissions = grantedPermissions,
                    },
                    "USER_INVITED",
                    "User invited successfully");
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<Result> RevokeAccess(int userId)
        {
            if (userId == _currentUser.UserId)
                return Result.Fail(
                    new Error("CANNOT_DELETE_SELF", "You cannot delete your own account", ErrorType.Validation));

            var user = await _repository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Id == userId,
            });

            if (user == null)
                return Result.Fail(new Error("USER_NOT_FOUND", "User not found", ErrorType.NotFound));

            var originalEmail = user.Email;
            var originalNickname = user.Nickname;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var revocation = await _roleManagementService.RevokeUserAccessAsync(userId);

                if (!revocation.Succeeded)
                {
                    await _unitOfWork.RollbackAsync();

                    return revocation.ErrorCode switch
                    {
                        AuthorizationErrorCodes.LastAdminProtected => Result.Fail(
                            new Error(revocation.ErrorCode, "The system must keep at least one administrator", ErrorType.Validation)),
                        _ => Result.Fail(new Error("USER_NOT_FOUND", "User not found", ErrorType.NotFound)),
                    };
                }

                user.Email = BuildTombstone(userId, originalEmail, MaxEmailLength);
                user.Nickname = BuildTombstone(userId, originalNickname, NicknameRules.MaxLength);
                user.IsActive = false;

                await _repository.SoftDelete(user);

                await _auditRepository.AddAsync(new AuthorizationAuditLog
                {
                    Action = AuthorizationAuditActions.UserAccessRevoked,
                    ActorUserId = _currentUser.UserId,
                    ActorEmail = _currentUser.Email,
                    TargetType = "User",
                    TargetId = userId.ToString(),
                    TargetName = user.Name,
                    OldValueJson = JsonSerializer.Serialize(new { Email = originalEmail, Nickname = originalNickname, Roles = revocation.Roles, Permissions = revocation.Permissions }),
                    IpAddress = _currentUser.IpAddress,
                });

                await _unitOfWork.CommitAsync();

                return Result.Ok("USER_DELETED", "User deleted successfully");
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private async Task<string> ResolveNicknameAsync(string email)
        {
            var baseNickname = NicknameRules.DeriveFromEmail(email);
            var candidate = baseNickname;

            for (var suffix = 2; NicknameRules.IsReserved(candidate) || await NicknameExistsAsync(candidate); suffix++)
            {
                var tail = suffix.ToString();
                var head = baseNickname[..Math.Min(baseNickname.Length, NicknameRules.MaxLength - tail.Length)].TrimEnd('.', '_');
                candidate = head + tail;
            }

            return candidate;
        }

        private async Task<bool> NicknameExistsAsync(string nickname)
        {
            var existing = await _repository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Nickname == nickname,
                IncludeDeleted = true,
                AsNoTracking = true,
            });

            return existing != null;
        }

        private static string BuildTombstone(int userId, string original, int maxLength)
        {
            var prefix = $"deleted+{userId}+";
            var available = maxLength - prefix.Length;

            return original.Length <= available
                ? prefix + original
                : prefix + original[..available];
        }

        private UserDto Map(User user, SignInMethods? methods) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Nickname = user.Nickname,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            AvatarUrl = _fileStorage.PublicUrl(user.AvatarKey),
            CityName = user.City?.Name ?? user.CityName,
            EmailConfirmed = methods?.EmailConfirmed ?? false,
            HasPassword = methods?.HasPassword ?? false,
            Providers = methods?.Providers.Select(p => (int)p).ToList() ?? [],
        };
    }
}
