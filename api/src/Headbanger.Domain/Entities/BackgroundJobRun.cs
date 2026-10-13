using Headbanger.Domain.Enums.Jobs;

namespace Headbanger.Domain.Entities
{
    public class BackgroundJobRun : EntityBase
    {
        public EBackgroundJobType JobType { get; set; }
        public EBackgroundJobStatus Status { get; set; } = EBackgroundJobStatus.Pending;
        public int? RequestedByUserId { get; set; }
        public User? RequestedByUser { get; set; }
        public string? RequestedByEmail { get; set; }
        public string? PayloadJson { get; set; }
        public string? DeduplicationKey { get; set; }
        public string? ResultJson { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResultStorageKey { get; set; }
        public string? HangfireJobId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
