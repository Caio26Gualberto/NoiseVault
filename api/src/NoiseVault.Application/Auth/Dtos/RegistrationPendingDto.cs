namespace NoiseVault.Application.Auth.Dtos
{
    public class RegistrationPendingDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
