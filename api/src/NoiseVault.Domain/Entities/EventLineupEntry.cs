namespace NoiseVault.Domain.Entities
{
    public class EventLineupEntry : EntityBase
    {
        public int EventId { get; set; }
        public int? BandId { get; set; }
        public string? Name { get; set; }
        public int Order { get; set; }

        public Event? Event { get; set; }
        public Band? Band { get; set; }
    }
}
