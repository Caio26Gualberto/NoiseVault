using NoiseVault.Application.Mentions.Dtos;
using NoiseVault.Application.BandOpenings.Dtos;
using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Events.Dtos;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Posts;

namespace NoiseVault.Application.Posts.Dtos
{
    public class PostDto
    {
        public int Id { get; set; }
        public ProfileSummaryDto Author { get; set; } = new();
        public BandSummaryDto? Band { get; set; }
        public EventSummaryDto? Event { get; set; }
        public ArchiveSummaryDto? Archive { get; set; }
        public BandOpeningSummaryDto? Opening { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<MentionDto> Mentions { get; set; } = [];
        public List<MediaDto> Media { get; set; } = [];
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool LikedByMe { get; set; }
        public EPostStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
