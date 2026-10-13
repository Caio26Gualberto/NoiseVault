namespace Headbanger.Application.Auth.Dtos
{
    public class ChangePasswordInputDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
