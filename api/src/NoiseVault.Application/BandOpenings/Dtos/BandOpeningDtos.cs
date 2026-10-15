using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Cities.Dtos;
using NoiseVault.Application.Musicians.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Domain.Enums.Ensaio;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.BandOpenings.Dtos
{
    public class BandOpeningDto
    {
        public int Id { get; set; }
        public BandSummaryDto Band { get; set; } = new();
        public CityDto? City { get; set; }
        public string? CityName { get; set; }
        public EInstrument Instrument { get; set; }
        public string? OtherName { get; set; }
        public string Description { get; set; } = string.Empty;
        public EMusicianGoal? Goal { get; set; }
        public EBandOpeningStatus Status { get; set; }
        public bool IsExpired { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool CanManage { get; set; }
        public OpeningMyInterestDto? MyInterest { get; set; }
        public int? InterestCount { get; set; }
        public int? DistanceKm { get; set; }
    }

    public class OpeningMyInterestDto
    {
        public EOpeningInterestStatus Status { get; set; }
        public string? Message { get; set; }
    }

    public class BandOpeningSummaryDto
    {
        public int Id { get; set; }
        public EInstrument Instrument { get; set; }
        public string? OtherName { get; set; }
        public string Description { get; set; } = string.Empty;
        public EBandOpeningStatus Status { get; set; }
        public bool IsExpired { get; set; }
    }

    public class OpeningInterestDto
    {
        public int Id { get; set; }
        public MusicianCardDto Musician { get; set; } = new();
        public string? Message { get; set; }
        public EOpeningInterestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BandOpeningInputDto
    {
        public EInstrument Instrument { get; set; }
        public string? OtherName { get; set; }
        public string? Description { get; set; }
        public EMusicianGoal? Goal { get; set; }
    }

    public class CloseOpeningInputDto
    {
        public bool Filled { get; set; }
    }

    public class OpeningInterestInputDto
    {
        public string? Message { get; set; }
    }

    public class InviteFromInterestInputDto
    {
        public string? Instrument { get; set; }
    }

    public class BandOpeningSearchQueryDto : PagedRequestDto
    {
        public EInstrument? Instrument { get; set; }
        public int? MaxKm { get; set; }
        public int? NearCityId { get; set; }
        public EMusicGenre? Genre { get; set; }
    }
}
