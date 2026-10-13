using Headbanger.Domain.Enums.Jobs;

namespace Headbanger.Application.BackgroundJobs.Dtos
{
    public class BackgroundJobListItemDto
    {
        public int Id { get; set; }
        public EBackgroundJobType JobType { get; set; }
        public EBackgroundJobStatus Status { get; set; }
        public int? RequestedByUserId { get; set; }
        public string? RequestedByName { get; set; }
        public string? RequestedByEmail { get; set; }
        public bool HasFile { get; set; }
        public string? ErrorMessage { get; set; }
        public int? DurationSeconds { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
