using NoiseVault.Application.Bands.Dtos;
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
        public string Content { get; set; } = string.Empty;
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
