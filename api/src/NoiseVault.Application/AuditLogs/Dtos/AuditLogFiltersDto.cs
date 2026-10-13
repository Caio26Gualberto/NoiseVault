namespace NoiseVault.Application.AuditLogs.Dtos
{
    public class AuditLogFiltersDto
    {
        public List<string> EntityTypes { get; set; } = [];
        public List<AuditLogActorDto> Actors { get; set; } = [];
    }

    public class AuditLogActorDto
    {
        public int AccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
