using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandVerificationQueryDto : PagedRequestDto
    {
        public EBandVerificationStatus? Status { get; set; }
        public string? Search { get; set; }
    }
}
