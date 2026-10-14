namespace NoiseVault.Domain.Entities
{
    public class ArchiveComment : EntityBase
    {
        public int ArchiveEntryId { get; set; }
        public int AuthorUserId { get; set; }
        public string Text { get; set; } = string.Empty;

        public ArchiveEntry? ArchiveEntry { get; set; }
        public User? Author { get; set; }
    }
}
