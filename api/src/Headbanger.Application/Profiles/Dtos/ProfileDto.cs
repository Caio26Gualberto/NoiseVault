namespace Headbanger.Application.Profiles.Dtos
{
    public class ProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Role { get; set; }
        public bool IsNeededChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
