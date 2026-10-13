using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.PublicProfiles.Dtos
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }
        public int BannerPosition { get; set; }
        public string? Bio { get; set; }
        public EAbcCity? City { get; set; }
        public string? MainInstrument { get; set; }
        public List<EMusicGenre> Genres { get; set; } = [];
        public SocialLinksDto SocialLinks { get; set; } = new();
        public DateTime MemberSince { get; set; }
        public List<BandSummaryDto> Bands { get; set; } = [];
    }
}
