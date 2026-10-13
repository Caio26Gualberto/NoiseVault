namespace NoiseVault.Domain.Entities
{
    public class Comment : EntityBase
    {
        public int PostId { get; set; }
        public int AuthorUserId { get; set; }
        public string Text { get; set; } = string.Empty;

        public Post? Post { get; set; }
        public User? Author { get; set; }
    }
}
