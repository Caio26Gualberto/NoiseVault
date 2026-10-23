namespace NoiseVault.Application.News.Dtos
{
    public class NewsImportPendingResponseDto
    {
        public int Links { get; set; }
        public List<NewsImportLinkDto> Pending { get; set; } = [];
    }
}
