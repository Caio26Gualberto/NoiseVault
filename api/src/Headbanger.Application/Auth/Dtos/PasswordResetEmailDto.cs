namespace Headbanger.Application.Auth.Dtos
{
    public class PasswordResetEmailDto
    {
        public string To { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
