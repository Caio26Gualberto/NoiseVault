namespace NoiseVault.Domain.Entities
{
    public class ArchiveMedia : EntityBase
    {
        public int ArchiveEntryId { get; set; }
        public int MediaUploadId { get; set; }
        public int Order { get; set; }
        public string? Caption { get; set; }

        public ArchiveEntry? ArchiveEntry { get; set; }
        public MediaUpload? MediaUpload { get; set; }
    }
}
