namespace NoiseVault.Domain.Entities
{
    public class EventComment : EntityBase
    {
        public int EventId { get; set; }
        public int AuthorUserId { get; set; }
        public string Text { get; set; } = string.Empty;

        public Event? Event { get; set; }
        public User? Author { get; set; }
    }
}
