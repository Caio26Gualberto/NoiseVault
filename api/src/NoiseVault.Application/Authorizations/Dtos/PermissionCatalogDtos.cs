namespace NoiseVault.Application.Authorizations.Dtos
{
    public class PermissionDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string? ParentName { get; set; }
    }

    public class PermissionGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<PermissionDto> Permissions { get; set; } = [];
    }
}
