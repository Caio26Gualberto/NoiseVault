using NoiseVault.Application.Pagination;

namespace NoiseVault.Application.Posts.Dtos
{
    public class FeedQueryDto : PagedRequestDto
    {
        public bool Following { get; set; }
    }
}
