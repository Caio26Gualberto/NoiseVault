namespace NoiseVault.Application.Bands.Dtos
{
    public class BandVerificationEmailDto
    {
        public string BandName { get; set; } = string.Empty;
        public string BandSlug { get; set; } = string.Empty;
        public string RequestedByName { get; set; } = string.Empty;
        public string RequestedByNickname { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
    }
}
