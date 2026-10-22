namespace NoiseVault.Application.News.Dtos
{
    public class NewsCollectResultDto
    {
        public int Added { get; set; }
        public int Candidates { get; set; }
        public int Failed { get; set; }
        public bool Blocked { get; set; }
        public bool Unavailable { get; set; }
        public bool Disabled { get; set; }
    }
}
