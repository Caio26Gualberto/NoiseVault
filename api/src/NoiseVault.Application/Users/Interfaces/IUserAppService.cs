using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Application.Users.Dtos;

namespace NoiseVault.Application.Users.Interfaces
{
    public interface IUserAppService
    {
        Task<Result<PagedResultDto<UserDto>>> GetAll(UserListQueryDto query);
        Task<Result<UserSignupSummaryDto>> Summary();
        Task<Result<UserDto>> Get(int userId);
        Task<Result<InvitedUserDto>> Invite(InviteUserInputDto input);
        Task<Result> RevokeAccess(int userId);
    }
}
