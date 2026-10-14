namespace NoiseVault.Application.Share.Dtos
{
    public class SharePreviewDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Type { get; set; } = "website";
    }
}
