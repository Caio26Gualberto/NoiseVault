namespace Headbanger.Application.Users.Dtos
{
    public class InviteUserInputDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public List<string>? AdditionalPermissions { get; set; }
    }
}
