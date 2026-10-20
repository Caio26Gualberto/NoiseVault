namespace NoiseVault.Application.Seo.Dtos
{
    public class SeoPageDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public string? ImageUrl { get; set; }
        public string Type { get; set; } = "website";
        public string Heading { get; set; } = string.Empty;
        public string? Lead { get; set; }
        public List<SeoFactDto> Facts { get; set; } = [];
        public List<string> Paragraphs { get; set; } = [];
        public List<SeoImageDto> Images { get; set; } = [];
        public List<SeoSectionDto> Sections { get; set; } = [];
        public object? StructuredData { get; set; }
    }

    public class SeoFactDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    public class SeoImageDto
    {
        public string Url { get; set; } = string.Empty;
        public string? Caption { get; set; }
    }

    public class SeoSectionDto
    {
        public string Title { get; set; } = string.Empty;
        public List<SeoLinkDto> Items { get; set; } = [];
    }

    public class SeoLinkDto
    {
        public string Text { get; set; } = string.Empty;
        public string? Path { get; set; }
        public string? Detail { get; set; }
    }

    public class SeoSitemapEntryDto
    {
        public string Path { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }
}
