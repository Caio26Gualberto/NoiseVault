namespace NoiseVault.Application.PublicProfiles.Dtos
{
    public class ProfileSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
