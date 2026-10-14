using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support.Dtos
{
    public class SupportTicketListQueryDto : PagedRequestDto
    {
        public ESupportTicketStatus? Status { get; set; }
        public ESupportTag? Tag { get; set; }
        public ESupportArea? Area { get; set; }
        public ESupportTicketPriority? Priority { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; }
    }
}
