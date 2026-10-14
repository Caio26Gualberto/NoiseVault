namespace NoiseVault.Application.Bands.Dtos
{
    public class BandInviteEmailDto
    {
        public string To { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string InvitedByName { get; set; } = string.Empty;
        public string BandName { get; set; } = string.Empty;
        public string? Instrument { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
