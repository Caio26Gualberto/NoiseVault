using NoiseVault.Domain.Enums.Events;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Events.Dtos
{
    public class EventSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? BannerUrl { get; set; }
        public int BannerPosition { get; set; }
        public DateTime StartsAt { get; set; }
        public string Venue { get; set; } = string.Empty;
        public EAbcCity City { get; set; }
        public string? CityName { get; set; }
        public EEventStatus Status { get; set; }
        public bool HasEnded { get; set; }
    }
}
