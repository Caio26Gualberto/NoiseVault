namespace NoiseVault.Domain.Entities
{
    public class PostLike
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
