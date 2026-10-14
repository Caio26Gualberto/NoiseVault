using NoiseVault.Domain.Enums.Events;

namespace NoiseVault.Domain.Entities
{
    public class Event : EntityBase
    {
        public const int DefaultBannerPosition = 50;

        public int OrganizerUserId { get; set; }
        public int? BandId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public DateTime EndsAtOrDefault { get; set; }
        public string Venue { get; set; } = string.Empty;
        public int? CityId { get; set; }
        public City? City { get; set; }
        public string? CityName { get; set; }
        public string? TicketsUrl { get; set; }
        public decimal? Price { get; set; }
        public int? Capacity { get; set; }
        public string? BannerKey { get; set; }
        public int BannerPosition { get; set; } = DefaultBannerPosition;
        public EEventStatus Status { get; set; }
        public DateTime? CancelledAt { get; set; }

        public User? Organizer { get; set; }
        public Band? Band { get; set; }
        public List<EventGenre> Genres { get; set; } = [];
        public List<EventLineupEntry> Lineup { get; set; } = [];
        public List<EventInterest> Interests { get; set; } = [];
        public List<EventComment> Comments { get; set; } = [];
    }
}
