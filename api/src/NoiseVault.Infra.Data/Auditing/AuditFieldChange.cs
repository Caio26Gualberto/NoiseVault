using System.Text.Json.Serialization;

namespace NoiseVault.Infra.Data.Auditing
{
    public class AuditFieldChange
    {
        public string Field { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OldValue { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NewValue { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Masked { get; set; }
    }
}
