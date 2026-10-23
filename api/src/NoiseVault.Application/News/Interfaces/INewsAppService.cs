using NoiseVault.Application.News.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.News.Interfaces
{
    public interface INewsAppService
    {
        Task<Result<PagedResultDto<NewsArticleDto>>> List(NewsListQueryDto query);
        Task<NewsCollectResultDto> Collect();
        Task<Result<NewsImportPendingResponseDto>> ImportPending(NewsImportPendingInputDto input);
        Task<Result<NewsImportResponseDto>> Import(NewsImportInputDto input);
        Task<int> Purge();
    }
}
