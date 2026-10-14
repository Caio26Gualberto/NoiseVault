using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Events.Dtos
{
    public class EventInputDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string? Venue { get; set; }
        public EAbcCity City { get; set; }
        public string? CityName { get; set; }
        public List<EMusicGenre>? Genres { get; set; }
        public string? TicketsUrl { get; set; }
        public decimal? Price { get; set; }
        public int? Capacity { get; set; }
        public List<EventLineupInputDto>? Lineup { get; set; }
    }
}
