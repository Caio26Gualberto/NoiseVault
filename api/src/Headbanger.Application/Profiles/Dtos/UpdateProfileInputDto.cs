using Headbanger.Domain.Enums.Profiles;

namespace Headbanger.Application.Profiles.Dtos
{
    public class UpdateProfileInputDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public EAbcCity? City { get; set; }
        public string? MainInstrument { get; set; }
        public List<EMusicGenre>? Genres { get; set; }
        public SocialLinksDto? SocialLinks { get; set; }
    }
}
