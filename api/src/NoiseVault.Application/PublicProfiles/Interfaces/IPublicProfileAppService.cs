using NoiseVault.Application.Pagination;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.PublicProfiles.Interfaces
{
    public interface IPublicProfileAppService
    {
        Task<Result<UserProfileDto>> GetByNickname(string nickname);
        Task<Result<PagedResultDto<ProfileSummaryDto>>> Search(UserSearchQueryDto query);

        Task<int?> FindUserId(string nickname);
        Task<Dictionary<string, ProfileSummaryDto>> SummariesByNicknames(IReadOnlyCollection<string> nicknames);
    }
}
