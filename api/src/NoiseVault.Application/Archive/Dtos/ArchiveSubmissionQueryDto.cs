using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Archive;

namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveSubmissionQueryDto : PagedRequestDto
    {
        public EArchiveEntryStatus? Status { get; set; }
        public string? Search { get; set; }
    }
}
