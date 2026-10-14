namespace NoiseVault.Domain.Entities
{
    public class EventInterest
    {
        public int EventId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
