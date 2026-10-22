using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.News.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.News;
using NoiseVault.Infra.Data.Context;

namespace NoiseVault.Infra.Data.Repositories.News
{
    public class NewsArticleRepository : Repository<NewsArticle>, INewsArticleRepository
    {
        public NewsArticleRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<HashSet<long>> ExistingExternalIdsAsync(ENewsSource source, IReadOnlyCollection<long> externalIds)
        {
            var existing = await _dbSet
                .IgnoreQueryFilters()
                .Where(n => n.Source == source && externalIds.Contains(n.ExternalId))
                .Select(n => n.ExternalId)
                .ToListAsync();

            return existing.ToHashSet();
        }

        public Task<int> PurgeAsync(DateTime publishedBefore) =>
            _dbSet
                .IgnoreQueryFilters()
                .Where(n => n.PublishedAt < publishedBefore)
                .ExecuteDeleteAsync();
    }
}
