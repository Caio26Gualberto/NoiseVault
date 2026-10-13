using Headbanger.Application.Pagination;

namespace Headbanger.Application.Users.Dtos
{
    public class UserListQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
