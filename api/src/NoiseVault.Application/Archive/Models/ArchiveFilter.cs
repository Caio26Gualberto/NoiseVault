using NoiseVault.Domain.Enums.Media;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Archive.Models
{
    public sealed record ArchiveFilter(
        int? Year = null,
        int? Decade = null,
        string? Search = null,
        int? BandId = null,
        int? CityId = null,
        EMediaKind? Kind = null,
        bool OldestFirst = false);
}
