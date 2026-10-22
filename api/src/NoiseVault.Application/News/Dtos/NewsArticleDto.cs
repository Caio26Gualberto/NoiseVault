using NoiseVault.Domain.Enums.News;

namespace NoiseVault.Application.News.Dtos
{
    public class NewsArticleDto
    {
        public int Id { get; set; }
        public ENewsSource Source { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Author { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
