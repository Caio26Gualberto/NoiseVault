namespace NoiseVault.Application.Auth.Dtos
{
    public class ExternalLinkInputDto
    {
        public string Ticket { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
