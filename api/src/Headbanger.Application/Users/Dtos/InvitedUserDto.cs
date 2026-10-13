namespace Headbanger.Application.Users.Dtos
{
    public class InvitedUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> AdditionalPermissions { get; set; } = [];
    }
}
