using NoiseVault.Application.Bands.Dtos;

namespace NoiseVault.Application.Events.Dtos
{
    public class EventLineupItemDto
    {
        public BandSummaryDto? Band { get; set; }
        public string? Name { get; set; }
    }
}
