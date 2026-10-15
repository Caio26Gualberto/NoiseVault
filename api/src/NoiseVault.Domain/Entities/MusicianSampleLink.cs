namespace NoiseVault.Domain.Entities
{
    public class MusicianSampleLink
    {
        public int Id { get; set; }
        public int MusicianProfileId { get; set; }
        public string Url { get; set; } = string.Empty;
        public int Position { get; set; }
    }
}
