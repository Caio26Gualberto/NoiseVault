namespace NoiseVault.Domain.Constants
{
    public static class Permissions
    {
        public static class Admin
        {
            public const string Access = "Admin.Access";
        }

        public static class Authorization
        {
            public const string UsersView = "Authorization.Users.View";
            public const string UsersInvite = "Authorization.Users.Invite";
            public const string UsersDelete = "Authorization.Users.Delete";
            public const string UsersAssignRole = "Authorization.Users.AssignRole";
            public const string UsersManagePermissions = "Authorization.Users.ManagePermissions";
            public const string RolesView = "Authorization.Roles.View";
            public const string RolesCreate = "Authorization.Roles.Create";
            public const string RolesEdit = "Authorization.Roles.Edit";
            public const string RolesDelete = "Authorization.Roles.Delete";
            public const string RolesManagePermissions = "Authorization.Roles.ManagePermissions";
            public const string AuditView = "Authorization.Audit.View";
        }

        public static class Archive
        {
            public const string Create = "Archive.Create";
            public const string Edit = "Archive.Edit";
            public const string Delete = "Archive.Delete";
        }

        public static class System
        {
            public const string JobsView = "System.Jobs.View";
            public const string JobsManage = "System.Jobs.Manage";
            public const string AuditView = "System.Audit.View";
        }
    }
}
