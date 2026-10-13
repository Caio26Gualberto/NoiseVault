using Headbanger.Application.Pagination;
using Headbanger.Application.Response;
using Headbanger.Application.Users.Dtos;

namespace Headbanger.Application.Users.Interfaces
{
    public interface IUserAppService
    {
        Task<Result<PagedResultDto<UserDto>>> GetAll(UserListQueryDto query);
        Task<Result<UserDto>> Get(int userId);
        Task<Result<InvitedUserDto>> Invite(InviteUserInputDto input);
        Task<Result> RevokeAccess(int userId);
    }
}
