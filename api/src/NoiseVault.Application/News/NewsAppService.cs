using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.News.Dtos;
using NoiseVault.Application.News.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.News;

namespace NoiseVault.Application.News
{
    public class NewsAppService : INewsAppService
    {
        private static readonly TimeSpan CollectPause = TimeSpan.FromSeconds(1);

        private readonly INewsArticleRepository _newsRepository;
        private readonly INewsSourceClient _sourceClient;
        private readonly IApplicationLogger _logger;

        public NewsAppService(INewsArticleRepository newsRepository, INewsSourceClient sourceClient, IApplicationLoggerFactory loggerFactory)
        {
            _newsRepository = newsRepository;
            _sourceClient = sourceClient;
            _logger = loggerFactory.Create(LogContext.News);
        }

        public async Task<Result<PagedResultDto<NewsArticleDto>>> List(NewsListQueryDto query)
        {
            query.Normalize();

            var scope = string.IsNullOrWhiteSpace(query.Scope) ? NewsRules.ScopeToday : query.Scope.Trim().ToLowerInvariant();

            if (scope != NewsRules.ScopeToday && scope != NewsRules.ScopeEarlier)
                return Result<PagedResultDto<NewsArticleDto>>.Fail(new Error("INVALID_NEWS_SCOPE", "Scope must be today or earlier", ErrorType.Validation));

            var todayStart = AppTime.TodayStartUtc;

            var options = new QueryOptions<NewsArticle>
            {
                Predicate = scope == NewsRules.ScopeToday
                    ? n => n.PublishedAt >= todayStart
                    : n => n.PublishedAt < todayStart,
                OrderBy = q => q.OrderByDescending(n => n.PublishedAt).ThenByDescending(n => n.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _newsRepository.CountAsync(options);
            var articles = await _newsRepository.ListAsync(options);

            return Result<PagedResultDto<NewsArticleDto>>.Ok(
                PagedResultDto<NewsArticleDto>.Create(articles.Select(Map).ToList(), query.Page, query.PageSize, totalCount),
                "NEWS_FOUND",
                "News found");
        }

        public async Task<NewsCollectResultDto> Collect()
        {
            _logger.SetOperation("CollectNews");

            var result = new NewsCollectResultDto();

            if (!_sourceClient.IsEnabled)
            {
                result.Disabled = true;
                return result;
            }

            IReadOnlyList<NewsLink> links;
            try
            {
                links = await _sourceClient.ListRecentAsync();
            }
            catch (NewsSourceException ex)
            {
                LogSourceFailure(ex, 0);
                result.Blocked = ex.Failure == ENewsSourceFailure.Blocked;
                result.Unavailable = ex.Failure != ENewsSourceFailure.Blocked;
                return result;
            }

            var existing = await _newsRepository.ExistingExternalIdsAsync(ENewsSource.Whiplash, links.Select(l => l.ExternalId).ToList());
            var candidates = links
                .Where(l => !existing.Contains(l.ExternalId))
                .OrderBy(l => l.ExternalId)
                .ToList();
            var batch = candidates.Count > NewsRules.MaxArticlesPerRun
                ? candidates.Skip(candidates.Count - NewsRules.MaxArticlesPerRun).ToList()
                : candidates;

            var oldestAccepted = DateTime.UtcNow.AddDays(-NewsRules.RetentionDays);
            var added = 0;

            for (var i = 0; i < batch.Count; i++)
            {
                var link = batch[i];

                if (i > 0)
                    await Task.Delay(CollectPause);

                NewsArticleData? data;
                try
                {
                    data = await _sourceClient.GetArticleAsync(link.Url);
                }
                catch (NewsSourceException ex) when (ex.Failure == ENewsSourceFailure.Blocked)
                {
                    LogSourceFailure(ex, added);
                    result.Blocked = true;
                    break;
                }
                catch (NewsSourceException ex)
                {
                    _logger.Warning("NEWS_ARTICLE_FAILED", "News article could not be read", extra: new { link.ExternalId, ex.Failure, ex.Message });
                    result.Failed++;
                    continue;
                }

                if (data == null || data.PublishedAt < oldestAccepted || link.Url.Length > NewsRules.UrlMaxLength)
                    continue;

                await _newsRepository.AddAsync(new NewsArticle
                {
                    Source = ENewsSource.Whiplash,
                    ExternalId = link.ExternalId,
                    Title = Truncate(data.Title, NewsRules.TitleMaxLength)!,
                    Url = link.Url,
                    ImageUrl = data.ImageUrl is { Length: <= NewsRules.UrlMaxLength } ? data.ImageUrl : null,
                    Author = Truncate(data.Author, NewsRules.AuthorMaxLength),
                    PublishedAt = data.PublishedAt,
                });

                added++;
            }

            _logger.Info("NEWS_COLLECTED", "News collected from Whiplash.Net", extra: new { Added = added, Candidates = candidates.Count });

            result.Added = added;
            result.Candidates = candidates.Count;
            return result;
        }

        public async Task<int> Purge()
        {
            var removed = await _newsRepository.PurgeAsync(DateTime.UtcNow.AddDays(-NewsRules.RetentionDays));

            _logger.Info("NEWS_PURGED", "Old news purged", extra: new { Removed = removed, NewsRules.RetentionDays });

            return removed;
        }

        private void LogSourceFailure(NewsSourceException ex, int added)
        {
            if (ex.Failure == ENewsSourceFailure.Blocked)
                _logger.Warning("NEWS_SOURCE_BLOCKED", "Whiplash.Net blocked the collector; the next run tries again", extra: new { Added = added, ex.Message });
            else
                _logger.Warning("NEWS_SOURCE_UNAVAILABLE", "Whiplash.Net sitemap could not be read", extra: new { ex.Failure, ex.Message });
        }

        private static string? Truncate(string? value, int maxLength) =>
            value == null || value.Length <= maxLength ? value : value[..maxLength];

        private static NewsArticleDto Map(NewsArticle article) => new()
        {
            Id = article.Id,
            Source = article.Source,
            Title = article.Title,
            Url = article.Url,
            ImageUrl = article.ImageUrl,
            Author = article.Author,
            PublishedAt = article.PublishedAt,
        };
    }
}
