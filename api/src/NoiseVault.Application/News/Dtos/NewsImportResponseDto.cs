namespace NoiseVault.Application.News.Dtos
{
    public class NewsImportResponseDto
    {
        public bool Added { get; set; }
        public bool AlreadyExists { get; set; }
        public bool TooOld { get; set; }
    }
}
