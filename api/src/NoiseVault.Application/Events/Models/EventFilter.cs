using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Events.Models
{
    public sealed record EventFilter(
        DateTime Now,
        bool Past = false,
        EMusicGenre? Genre = null,
        EAbcCity? City = null,
        string? Search = null,
        bool InterestedOnly = false,
        int? BandId = null,
        int? OrganizerUserId = null);
}
