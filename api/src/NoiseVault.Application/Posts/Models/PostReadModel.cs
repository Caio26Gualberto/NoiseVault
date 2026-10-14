namespace NoiseVault.Application.Posts.Models
{
    public sealed class PostReadModel
    {
        public int Id { get; init; }
        public int AuthorUserId { get; init; }
        public string AuthorName { get; init; } = string.Empty;
        public string AuthorNickname { get; init; } = string.Empty;
        public string? AuthorAvatarKey { get; init; }
        public int? BandId { get; init; }
        public int? EventId { get; init; }
        public string Content { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime? PublishedAt { get; init; }
        public DateTime? EditedAt { get; init; }
        public int LikeCount { get; init; }
        public int CommentCount { get; init; }
        public bool LikedByMe { get; init; }
        public bool HasFailedMedia { get; init; }
        public bool ViewerOwnsBand { get; init; }
    }
}
