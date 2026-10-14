using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Events;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Events.Dtos
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public string Venue { get; set; } = string.Empty;
        public EAbcCity City { get; set; }
        public string? CityName { get; set; }
        public string? BannerUrl { get; set; }
        public int BannerPosition { get; set; }
        public List<EMusicGenre> Genres { get; set; } = [];
        public string? TicketsUrl { get; set; }
        public decimal? Price { get; set; }
        public int? Capacity { get; set; }
        public EEventStatus Status { get; set; }
        public DateTime? CancelledAt { get; set; }
        public bool HasEnded { get; set; }
        public ProfileSummaryDto Organizer { get; set; } = new();
        public BandSummaryDto? Band { get; set; }
        public List<EventLineupItemDto> Lineup { get; set; } = [];
        public int InterestedCount { get; set; }
        public bool Interested { get; set; }
        public int CommentCount { get; set; }
        public bool CanManage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
