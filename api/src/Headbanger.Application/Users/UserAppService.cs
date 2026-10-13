using System.Text.Json;
using Headbanger.Application.Abstractions.Authentication;
using Headbanger.Application.Abstractions.Authorization;
using Headbanger.Application.Abstractions.Repository;
using Headbanger.Application.Abstractions.Repository.UnitOfWork;
using Headbanger.Application.Auth;
using Headbanger.Application.Interfaces;
using Headbanger.Application.Pagination;
using Headbanger.Application.Response;
using Headbanger.Application.Users.Dtos;
using Headbanger.Application.Users.Interfaces;
using Headbanger.Domain.Authorization;
using Headbanger.Domain.Constants;
using Headbanger.Domain.Entities;
using Headbanger.Domain.Interfaces.Authenticate;

namespace Headbanger.Application.Users
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

        public UserAppService(
            IRepository<User> repository,
            IRepository<AuthorizationAuditLog> auditRepository,
            IAuthenticateService authService,
            IEmailService emailService,
            IRoleManagementService roleManagementService,
            IPermissionManager permissionManager,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _auditRepository = auditRepository;
            _authService = authService;
            _emailService = emailService;
            _roleManagementService = roleManagementService;
            _permissionManager = permissionManager;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<PagedResultDto<UserDto>>> GetAll(UserListQueryDto query)
        {
            query.Normalize();

            var search = query.Search?.Trim();

            var options = new QueryOptions<User>
            {
                Predicate = u => string.IsNullOrEmpty(search) || u.Name.Contains(search) || u.Email.Contains(search),
                OrderBy = q => q.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _repository.CountAsync(options);
            var users = await _repository.ListAsync(options);

            var page = PagedResultDto<UserDto>.Create(users.Select(Map).ToList(), query.Page, query.PageSize, totalCount);

            return Result<PagedResultDto<UserDto>>.Ok(page, "USERS_LOADED", "Users loaded successfully");
        }

        public async Task<Result<UserDto>> Get(int userId)
        {
            var user = await _repository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Id == userId,
                AsNoTracking = true,
            });

            if (user == null)
                return Result<UserDto>.Fail(new Error("USER_NOT_FOUND", "User not found", ErrorType.NotFound));

            return Result<UserDto>.Ok(Map(user), "USER_FOUND", "User found successfully");
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

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var creation = await _authService.CreateUser(email, temporaryPassword, name, role.Name);

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

                user.Email = BuildTombstoneEmail(userId, originalEmail);
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
                    OldValueJson = JsonSerializer.Serialize(new { Email = originalEmail, Roles = revocation.Roles, Permissions = revocation.Permissions }),
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

        private static string BuildTombstoneEmail(int userId, string originalEmail)
        {
            var prefix = $"deleted+{userId}+";
            var available = MaxEmailLength - prefix.Length;

            return originalEmail.Length <= available
                ? prefix + originalEmail
                : prefix + originalEmail[..available];
        }

        private static UserDto Map(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
        };
    }
}
