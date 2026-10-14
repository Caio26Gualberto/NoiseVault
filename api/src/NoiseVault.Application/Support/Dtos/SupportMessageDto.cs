using NoiseVault.Application.PublicProfiles.Dtos;

namespace NoiseVault.Application.Support.Dtos
{
    public class SupportMessageDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool FromStaff { get; set; }
        public bool Mine { get; set; }
        public ProfileSummaryDto Author { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
