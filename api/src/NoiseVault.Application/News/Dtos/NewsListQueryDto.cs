using NoiseVault.Application.Pagination;

namespace NoiseVault.Application.News.Dtos
{
    public class NewsListQueryDto : PagedRequestDto
    {
        public string? Scope { get; set; }
    }
}
