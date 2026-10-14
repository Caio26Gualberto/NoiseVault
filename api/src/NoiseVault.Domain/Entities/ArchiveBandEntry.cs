namespace NoiseVault.Domain.Entities
{
    public class ArchiveBandEntry : EntityBase
    {
        public int ArchiveEntryId { get; set; }
        public int? BandId { get; set; }
        public string? Name { get; set; }
        public int Order { get; set; }

        public ArchiveEntry? ArchiveEntry { get; set; }
        public Band? Band { get; set; }
    }
}
