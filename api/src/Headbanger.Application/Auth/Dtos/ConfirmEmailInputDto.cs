namespace Headbanger.Application.Auth.Dtos
{
    public class ConfirmEmailInputDto
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
