using NoiseVault.Application.Pagination;

namespace NoiseVault.Application.Cities.Dtos
{
    public class CitySearchQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
