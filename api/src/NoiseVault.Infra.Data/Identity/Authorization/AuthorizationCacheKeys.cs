namespace NoiseVault.Infra.Data.Identity.Authorization
{
    public static class AuthorizationCacheKeys
    {
        public const string Tag = "authz:perms";

        public static string UserKey(int userId) => $"authz:perms:user:{userId}";

        public static string RoleTag(int roleId) => $"authz:perms:role:{roleId}";
    }
}
