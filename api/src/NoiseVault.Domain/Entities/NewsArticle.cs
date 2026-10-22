using NoiseVault.Domain.Enums.News;

namespace NoiseVault.Domain.Entities
{
    public class NewsArticle : EntityBase
    {
        public ENewsSource Source { get; set; }
        public long ExternalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Author { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
