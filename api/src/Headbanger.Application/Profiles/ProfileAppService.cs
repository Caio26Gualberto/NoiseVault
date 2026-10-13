using Headbanger.Application.Abstractions.Authentication;
using Headbanger.Application.Abstractions.Repository;
using Headbanger.Application.Auth;
using Headbanger.Application.Profiles.Dtos;
using Headbanger.Application.Profiles.Interfaces;
using Headbanger.Application.Response;
using Headbanger.Domain.Entities;
using Headbanger.Domain.Interfaces.Authenticate;

namespace Headbanger.Application.Profiles
{
    public class ProfileAppService : IProfileAppService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IAuthenticateService _authService;
        private readonly ICurrentUser _currentUser;

        public ProfileAppService(
            IRepository<User> userRepository,
            IAuthenticateService authService,
            ICurrentUser currentUser)
        {
            _userRepository = userRepository;
            _authService = authService;
            _currentUser = currentUser;
        }

        public async Task<Result<ProfileDto>> Me()
        {
            var user = await _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Id == _currentUser.UserId,
                AsNoTracking = true
            });

            if (user == null)
                return Result<ProfileDto>.Fail(UserNotFound());

            return Result<ProfileDto>.Ok(await MapAsync(user), "USER_FOUND", "User found successfully");
        }

        public async Task<Result<ProfileDto>> UpdateName(UpdateProfileNameDto input)
        {
            var name = UserNameRules.Normalize(input.Name);

            if (!UserNameRules.IsValid(name))
                return Result<ProfileDto>.Fail(new Error(
                    "INVALID_NAME",
                    $"Name must be between {UserNameRules.MinLength} and {UserNameRules.MaxLength} characters",
                    ErrorType.Validation));

            var user = await FindCurrentAsync();

            if (user == null)
                return Result<ProfileDto>.Fail(UserNotFound());

            user.Name = name;
            await _userRepository.UpdateAsync(user);

            return Result<ProfileDto>.Ok(await MapAsync(user), "PROFILE_UPDATED", "Profile updated successfully");
        }

        private Task<User?> FindCurrentAsync() =>
            _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Id == _currentUser.UserId
            });

        private static Error UserNotFound() => new("USER_NOT_FOUND", "User not found", ErrorType.NotFound);

        private async Task<ProfileDto> MapAsync(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Nickname = user.Nickname,
            Email = user.Email,
            Role = _currentUser.Role,
            IsNeededChangePassword = await _authService.IsPasswordChangeRequired(user.Email),
            CreatedAt = user.CreatedAt
        };
    }
}
