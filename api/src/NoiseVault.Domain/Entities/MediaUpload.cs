using NoiseVault.Domain.Enums.Media;

namespace NoiseVault.Domain.Entities
{
    public class MediaUpload : EntityBase
    {
        public int OwnerUserId { get; set; }
        public EMediaKind Kind { get; set; }
        public EMediaProfile Profile { get; set; }
        public EMediaStatus Status { get; set; }
        public string? StorageKey { get; set; }
        public string? OriginalKey { get; set; }
        public string? MultipartUploadId { get; set; }
        public string? PosterKey { get; set; }
        public string? TranscodeJobId { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? DurationSeconds { get; set; }
        public long SizeBytes { get; set; }
        public string? FailureReason { get; set; }
        public DateTime ExpiresAt { get; set; }

        public User? Owner { get; set; }
        public PostMedia? Attachment { get; set; }
        public ArchiveMedia? ArchiveAttachment { get; set; }
        public SupportTicketMedia? SupportAttachment { get; set; }
    }
}
