using NoiseVault.Application.Pagination;

namespace NoiseVault.Application.PublicProfiles.Dtos
{
    public class UserSearchQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
