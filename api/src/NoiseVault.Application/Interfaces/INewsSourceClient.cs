namespace NoiseVault.Application.Interfaces
{
    public sealed record NewsLink(long ExternalId, string Url);

    public sealed record NewsArticleData(string Title, string? ImageUrl, string? Author, DateTime PublishedAt);

    public enum ENewsSourceFailure
    {
        Blocked = 0,
        Unavailable = 1,
    }

    public sealed class NewsSourceException : Exception
    {
        public ENewsSourceFailure Failure { get; }

        public NewsSourceException(ENewsSourceFailure failure, string message, Exception? inner = null) : base(message, inner)
        {
            Failure = failure;
        }
    }

    public interface INewsSourceClient
    {
        bool IsEnabled { get; }
        Task<IReadOnlyList<NewsLink>> ListRecentAsync(CancellationToken cancellationToken = default);
        Task<NewsArticleData?> GetArticleAsync(string url, CancellationToken cancellationToken = default);
        IReadOnlyList<NewsLink> ParseSitemap(string body);
        NewsLink? ParseLink(string url);
        NewsArticleData? ParseArticle(string html);
    }
}
