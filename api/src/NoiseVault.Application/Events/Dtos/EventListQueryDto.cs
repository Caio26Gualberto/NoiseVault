using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Events.Dtos
{
    public class EventListQueryDto : PagedRequestDto
    {
        public string? Scope { get; set; }
        public EMusicGenre? Genre { get; set; }
        public EAbcCity? City { get; set; }
        public string? Search { get; set; }
        public bool Interested { get; set; }
    }
}
