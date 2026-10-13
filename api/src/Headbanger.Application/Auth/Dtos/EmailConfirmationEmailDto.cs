namespace Headbanger.Application.Auth.Dtos
{
    public class EmailConfirmationEmailDto
    {
        public string To { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
