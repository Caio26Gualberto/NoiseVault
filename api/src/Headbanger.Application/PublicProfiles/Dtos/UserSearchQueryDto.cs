using Headbanger.Application.Pagination;

namespace Headbanger.Application.PublicProfiles.Dtos
{
    public class UserSearchQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
