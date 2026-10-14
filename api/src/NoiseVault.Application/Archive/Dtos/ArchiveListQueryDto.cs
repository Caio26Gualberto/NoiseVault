using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Archive.Dtos
{
    public class ArchiveListQueryDto : PagedRequestDto
    {
        public int? Year { get; set; }
        public int? Decade { get; set; }
        public string? Search { get; set; }
        public EAbcCity? City { get; set; }
        public string? Kind { get; set; }
        public string? Sort { get; set; }
    }
}
