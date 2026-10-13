namespace NoiseVault.Application.Abstractions.Authorization
{
    public static class AuthorizationErrorCodes
    {
        public const string RoleNotFound = "ROLE_NOT_FOUND";
        public const string RoleNameTaken = "ROLE_NAME_TAKEN";
        public const string RoleInUse = "ROLE_IN_USE";
        public const string SystemRoleProtected = "SYSTEM_ROLE_PROTECTED";
        public const string LastAdminProtected = "LAST_ADMIN_PROTECTED";
        public const string UnknownPermission = "UNKNOWN_PERMISSION";
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string AdminRoleImplicit = "ADMIN_ROLE_IMPLICIT";
    }
}
