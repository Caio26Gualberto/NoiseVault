using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.News;

namespace NoiseVault.Application.News.Interfaces
{
    public interface INewsArticleRepository : IRepository<NewsArticle>
    {
        Task<HashSet<long>> ExistingExternalIdsAsync(ENewsSource source, IReadOnlyCollection<long> externalIds);
        Task<int> PurgeAsync(DateTime publishedBefore);
    }
}
