namespace NoiseVault.Domain.Constants
{
    public static class AuthorizationAuditActions
    {
        public const string RoleCreated = "RoleCreated";
        public const string RoleUpdated = "RoleUpdated";
        public const string RoleDeleted = "RoleDeleted";
        public const string RolePermissionsChanged = "RolePermissionsChanged";
        public const string UserInvited = "UserInvited";
        public const string UserAccessRevoked = "UserAccessRevoked";
        public const string UserRoleChanged = "UserRoleChanged";
        public const string UserPermissionsChanged = "UserPermissionsChanged";
    }
}
