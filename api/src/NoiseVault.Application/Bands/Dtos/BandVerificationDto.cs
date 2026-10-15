using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands.Dtos
{
    public class BandVerificationDto
    {
        public int Id { get; set; }
        public BandSummaryDto Band { get; set; } = new();
        public SocialLinksDto BandSocialLinks { get; set; } = new();
        public int BandMemberCount { get; set; }
        public ProfileSummaryDto RequestedBy { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public EBandVerificationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public ProfileSummaryDto? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? RejectionReason { get; set; }
    }
}
