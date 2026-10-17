namespace NoiseVault.Application.Auth.Dtos
{
    public class ExternalRegisterInputDto
    {
        public string Ticket { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
