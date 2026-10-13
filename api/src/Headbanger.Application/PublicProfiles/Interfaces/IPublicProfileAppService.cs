using Headbanger.Application.Pagination;
using Headbanger.Application.PublicProfiles.Dtos;
using Headbanger.Application.Response;

namespace Headbanger.Application.PublicProfiles.Interfaces
{
    public interface IPublicProfileAppService
    {
        Task<Result<UserProfileDto>> GetByNickname(string nickname);
        Task<Result<PagedResultDto<ProfileSummaryDto>>> Search(UserSearchQueryDto query);
    }
}
