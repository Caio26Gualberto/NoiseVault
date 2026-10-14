namespace NoiseVault.Application.Events.Dtos
{
    public class CreateEventInputDto : EventInputDto
    {
        public string? BandSlug { get; set; }
    }
}
