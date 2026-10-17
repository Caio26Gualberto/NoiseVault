namespace NoiseVault.Application.Auth.Dtos
{
    public class ExternalLoginInputDto
    {
        public int Provider { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
