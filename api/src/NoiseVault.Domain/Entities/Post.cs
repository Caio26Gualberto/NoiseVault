namespace NoiseVault.Domain.Entities
{
    public class Post : EntityBase
    {
        public int AuthorUserId { get; set; }
        public int? BandId { get; set; }
        public int? EventId { get; set; }
        public int? ArchiveEntryId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public DateTime? EditedAt { get; set; }

        public User? Author { get; set; }
        public Band? Band { get; set; }
        public Event? Event { get; set; }
        public ArchiveEntry? ArchiveEntry { get; set; }
        public List<PostMedia> Media { get; set; } = [];
        public List<PostLike> Likes { get; set; } = [];
        public List<Comment> Comments { get; set; } = [];
    }
}
