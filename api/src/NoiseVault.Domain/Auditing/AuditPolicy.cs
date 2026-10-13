using NoiseVault.Domain.Entities;

namespace NoiseVault.Domain.Auditing
{
    public static class AuditPolicy
    {
        public const string MaskedPlaceholder = "***";

        private static readonly HashSet<string> Ignored =
        [
            nameof(AuditLog),
            nameof(AuthorizationAuditLog),
            nameof(BackgroundJobRun),
        ];

        private static readonly HashSet<string> AlwaysOmittedProperties =
        [
            nameof(EntityBase.Id),
            nameof(EntityBase.CreatedAt),
            nameof(EntityBase.CreatedBy),
            nameof(EntityBase.UpdatedAt),
            nameof(EntityBase.UpdatedBy),
            nameof(EntityBase.DeletedAt),
            nameof(EntityBase.DeletedBy),
            nameof(EntityBase.IsDeleted),
        ];

        private static readonly Dictionary<string, HashSet<string>> Masked = new();

        private static readonly string[] DescriptionProperties =
        [
            "Name",
            "Title",
            "Code",
        ];

        public static bool IsAudited(string entityType) => !Ignored.Contains(entityType);

        public static bool IsOmitted(string propertyName) => AlwaysOmittedProperties.Contains(propertyName);

        public static bool IsMasked(string entityType, string propertyName) =>
            Masked.TryGetValue(entityType, out var properties) && properties.Contains(propertyName);

        public static bool IsDescriptionProperty(string propertyName) =>
            DescriptionProperties.Contains(propertyName);

        public static int DescriptionPriority(string propertyName) =>
            Array.IndexOf(DescriptionProperties, propertyName);
    }
}
