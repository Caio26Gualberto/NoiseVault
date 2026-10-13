using Headbanger.Application.Pagination;
using Headbanger.Domain.Enums.Jobs;

namespace Headbanger.Application.BackgroundJobs.Dtos
{
    public class BackgroundJobQueryDto : PagedRequestDto
    {
        public EBackgroundJobType? JobType { get; set; }
        public EBackgroundJobStatus? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
