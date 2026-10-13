namespace Headbanger.Application.Auth.Dtos
{
    public class LoginResponseDto
    {
        public TokensDto? Tokens { get; set; }
        public bool IsNeededChangePassword { get; set; }
    }
}
