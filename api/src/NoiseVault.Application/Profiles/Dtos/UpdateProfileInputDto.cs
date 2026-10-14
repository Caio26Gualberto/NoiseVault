using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Profiles.Dtos
{
    public class UpdateProfileInputDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
        public string? MainInstrument { get; set; }
        public List<EMusicGenre>? Genres { get; set; }
        public SocialLinksDto? SocialLinks { get; set; }
    }
}
