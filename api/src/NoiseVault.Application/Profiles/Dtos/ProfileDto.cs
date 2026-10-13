using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Profiles.Dtos
{
    public class ProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Role { get; set; }
        public bool IsNeededChangePassword { get; set; }
        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }
        public int BannerPosition { get; set; }
        public string? Bio { get; set; }
        public EAbcCity? City { get; set; }
        public string? MainInstrument { get; set; }
        public List<EMusicGenre> Genres { get; set; } = [];
        public SocialLinksDto SocialLinks { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
