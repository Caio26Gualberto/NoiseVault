namespace NoiseVault.Application.Auth.Dtos
{
    public class ExternalLoginResponseDto
    {
        public EExternalLoginOutcome Outcome { get; set; }
        public TokensDto? Tokens { get; set; }
        public string? Ticket { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? SuggestedNickname { get; set; }
        public bool HasPassword { get; set; }
    }
}
