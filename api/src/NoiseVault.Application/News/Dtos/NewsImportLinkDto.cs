namespace NoiseVault.Application.News.Dtos
{
    public class NewsImportLinkDto
    {
        public long ExternalId { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
