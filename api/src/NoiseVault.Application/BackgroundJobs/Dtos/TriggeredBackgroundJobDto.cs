using NoiseVault.Domain.Enums.Jobs;

namespace NoiseVault.Application.BackgroundJobs.Dtos
{
    public class TriggeredBackgroundJobDto
    {
        public int Id { get; set; }
        public EBackgroundJobType JobType { get; set; }
        public EBackgroundJobStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
