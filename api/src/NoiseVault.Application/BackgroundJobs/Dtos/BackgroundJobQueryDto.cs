using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Jobs;

namespace NoiseVault.Application.BackgroundJobs.Dtos
{
    public class BackgroundJobQueryDto : PagedRequestDto
    {
        public EBackgroundJobType? JobType { get; set; }
        public EBackgroundJobStatus? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
