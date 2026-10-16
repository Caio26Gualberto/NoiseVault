namespace NoiseVault.Domain.Entities
{
    public class BandFollower
    {
        public int BandId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Band? Band { get; set; }
        public User? User { get; set; }
    }
}
