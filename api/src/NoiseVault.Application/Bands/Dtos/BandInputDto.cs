using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandInputDto
    {
        public EBandKind? Kind { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public List<EMusicGenre>? Genres { get; set; }
        public int? FoundedYear { get; set; }
        public SocialLinksDto? SocialLinks { get; set; }
    }
}
