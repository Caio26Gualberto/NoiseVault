namespace Headbanger.Application.Authorizations.Dtos
{
    public class RoleRefDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AdminUserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UserAccessDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<RoleRefDto> Roles { get; set; } = [];
        public List<string> RolePermissions { get; set; } = [];
        public List<string> UserPermissions { get; set; } = [];
        public List<string> EffectivePermissions { get; set; } = [];
    }

    public class MyAccessDto
    {
        public List<RoleRefDto> Roles { get; set; } = [];
        public List<string> RolePermissions { get; set; } = [];
        public List<string> UserPermissions { get; set; } = [];
        public List<string> EffectivePermissions { get; set; } = [];
    }

    public class RoleChangePreviewDto
    {
        public List<string> Added { get; set; } = [];
        public List<string> Removed { get; set; } = [];
        public List<string> Preserved { get; set; } = [];
    }

    public class UpdateUserRoleRequestDto
    {
        public int RoleId { get; set; }
    }

    public class UpdateUserPermissionsRequestDto
    {
        public List<string> Permissions { get; set; } = [];
    }
}
