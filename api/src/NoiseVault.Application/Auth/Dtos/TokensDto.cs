namespace NoiseVault.Application.Auth.Dtos
{
    public class TokensDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
