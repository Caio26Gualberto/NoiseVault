using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandSearchQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
        public EBandKind? Kind { get; set; }
    }
}
