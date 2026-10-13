using NoiseVault.Application.Pagination;

namespace NoiseVault.Application.Users.Dtos
{
    public class UserListQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
