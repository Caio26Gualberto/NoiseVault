using System.Text.Json.Serialization;

namespace NoiseVault.Application.Auth.Dtos
{
    public class TokensDto
    {
        public string Token { get; set; } = string.Empty;

        [JsonIgnore]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
