namespace NoiseVault.Application.Users.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AvatarUrl { get; set; }
        public string? CityName { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool HasPassword { get; set; }
        public List<int> Providers { get; set; } = [];
    }
}
