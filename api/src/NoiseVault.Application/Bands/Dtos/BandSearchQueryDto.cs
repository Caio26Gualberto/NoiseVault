using NoiseVault.Application.Pagination;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandSearchQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
