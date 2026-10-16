using NoiseVault.Application.Cities.Dtos;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CityDto? City { get; set; }
        public string? CityName { get; set; }
        public List<EMusicGenre> Genres { get; set; } = [];
        public int? FoundedYear { get; set; }
        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }
        public int BannerPosition { get; set; }
        public bool IsVerified { get; set; }
        public SocialLinksDto SocialLinks { get; set; } = new();
        public List<BandMemberDto> Members { get; set; } = [];
        public EBandMemberRole? MyRole { get; set; }
        public BandVerificationStateDto? Verification { get; set; }
        public int PostCount { get; set; }
        public int UpcomingEventCount { get; set; }
        public int FollowerCount { get; set; }
        public bool FollowedByMe { get; set; }
    }
}
