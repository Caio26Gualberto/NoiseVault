namespace NoiseVault.Application.Auth.Dtos
{
    public class ResetPasswordInputDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
