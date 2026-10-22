using NoiseVault.Application.News.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.News.Interfaces
{
    public interface INewsAppService
    {
        Task<Result<PagedResultDto<NewsArticleDto>>> List(NewsListQueryDto query);
        Task<int> Collect();
        Task<int> Purge();
    }
}
