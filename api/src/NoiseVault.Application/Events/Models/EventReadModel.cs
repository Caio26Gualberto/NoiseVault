using NoiseVault.Domain.Enums.Events;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Events.Models
{
    public sealed class EventReadModel
    {
        public int Id { get; init; }
        public int OrganizerUserId { get; init; }
        public string OrganizerName { get; init; } = string.Empty;
        public string OrganizerNickname { get; init; } = string.Empty;
        public string? OrganizerAvatarKey { get; init; }
        public int? BandId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTime StartsAt { get; init; }
        public DateTime? EndsAt { get; init; }
        public DateTime EndsAtOrDefault { get; init; }
        public string Venue { get; init; } = string.Empty;
        public EAbcCity City { get; init; }
        public string? CityName { get; init; }
        public string? TicketsUrl { get; init; }
        public decimal? Price { get; init; }
        public int? Capacity { get; init; }
        public string? BannerKey { get; init; }
        public int BannerPosition { get; init; }
        public EEventStatus Status { get; init; }
        public DateTime? CancelledAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<EMusicGenre> Genres { get; init; } = [];
        public List<EventLineupRow> Lineup { get; init; } = [];
        public int InterestedCount { get; init; }
        public bool Interested { get; init; }
        public int CommentCount { get; init; }
        public bool ViewerOwnsBand { get; init; }
    }

    public sealed class EventLineupRow
    {
        public int? BandId { get; init; }
        public string? Name { get; init; }
        public int Order { get; init; }
    }
}
