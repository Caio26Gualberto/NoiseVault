using NoiseVault.Application.Cities.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Domain.Enums.Ensaio;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Musicians.Dtos
{
    public class MusicianInstrumentDto
    {
        public EInstrument Instrument { get; set; }
        public string? OtherName { get; set; }
        public ESkillLevel Level { get; set; }
    }

    public class AvailabilitySlotDto
    {
        public EWeekDay Day { get; set; }
        public EDayPeriod Period { get; set; }
    }

    public class MusicianProfileDto
    {
        public bool Exists { get; set; }
        public bool IsMine { get; set; }
        public ProfileSummaryDto Person { get; set; } = new();
        public CityDto? City { get; set; }
        public string? CityName { get; set; }
        public bool IsLookingForBand { get; set; }
        public DateTime? LookingSince { get; set; }
        public EMusicianGoal Goal { get; set; }
        public EMusicianCommitment Commitment { get; set; }
        public EReach Reach { get; set; }
        public bool HasGear { get; set; }
        public bool HasTransport { get; set; }
        public bool HasRehearsalSpace { get; set; }
        public string? Influences { get; set; }
        public List<MusicianInstrumentDto> Instruments { get; set; } = [];
        public List<EMusicGenre> Genres { get; set; } = [];
        public List<AvailabilitySlotDto> Availability { get; set; } = [];
        public List<string> SampleLinks { get; set; } = [];
    }

    public class MusicianCardDto
    {
        public ProfileSummaryDto Person { get; set; } = new();
        public CityDto? City { get; set; }
        public string? CityName { get; set; }
        public MusicianInstrumentDto? Instrument { get; set; }
        public List<EMusicGenre> Genres { get; set; } = [];
        public bool HasSamples { get; set; }
        public DateTime? LookingSince { get; set; }
    }

    public class MusicianInputDto
    {
        public bool IsLookingForBand { get; set; }
        public EMusicianGoal Goal { get; set; }
        public EMusicianCommitment Commitment { get; set; }
        public EReach Reach { get; set; }
        public bool HasGear { get; set; }
        public bool HasTransport { get; set; }
        public bool HasRehearsalSpace { get; set; }
        public string? Influences { get; set; }
        public List<MusicianInstrumentDto>? Instruments { get; set; }
        public List<EMusicGenre>? Genres { get; set; }
        public List<AvailabilitySlotDto>? Availability { get; set; }
        public List<string>? SampleLinks { get; set; }
    }

    public class MusicianLookingInputDto
    {
        public bool IsLookingForBand { get; set; }
    }

    public class MusicianSearchQueryDto : PagedRequestDto
    {
        public EInstrument? Instrument { get; set; }
        public int? CityId { get; set; }
        public bool? Abc { get; set; }
        public EMusicGenre? Genre { get; set; }
        public bool? WithSamples { get; set; }
    }
}
