using Headbanger.Domain.Constants;

namespace Headbanger.Domain.Authorization
{
    public static class PermissionCatalog
    {
        public static class Groups
        {
            public const string Admin = "Admin";
            public const string Authorization = "Authorization";
            public const string System = "System";
        }

        public static readonly IReadOnlyList<PermissionGroupDefinition> GroupDefinitions = new List<PermissionGroupDefinition>
        {
            new(Groups.Admin, "Geral"),
            new(Groups.Authorization, "Equipe e acesso"),
            new(Groups.System, "Sistema"),
        };

        public static readonly IReadOnlyList<PermissionDefinition> All = new List<PermissionDefinition>
        {
            new(Permissions.Admin.Access, "Acessar o painel", "Permite entrar no painel administrativo.", Groups.Admin),


            new(Permissions.Authorization.UsersView, "Ver usuários da equipe", "Permite visualizar os usuários com acesso ao painel.", Groups.Authorization),
            new(Permissions.Authorization.UsersInvite, "Convidar pessoa para a equipe", "Permite convidar uma nova pessoa para o painel, definindo papel e permissões adicionais.", Groups.Authorization, Permissions.Authorization.UsersView),
            new(Permissions.Authorization.UsersDelete, "Excluir pessoa da equipe", "Permite remover o acesso de uma pessoa ao painel, preservando o histórico das ações dela.", Groups.Authorization, Permissions.Authorization.UsersView),
            new(Permissions.Authorization.UsersAssignRole, "Atribuir papel a usuário", "Permite alterar o papel principal de um usuário da equipe.", Groups.Authorization, Permissions.Authorization.UsersView),
            new(Permissions.Authorization.UsersManagePermissions, "Gerenciar permissões de usuário", "Permite conceder ou remover permissões adicionais de um usuário.", Groups.Authorization, Permissions.Authorization.UsersView),
            new(Permissions.Authorization.RolesView, "Ver papéis", "Permite visualizar os papéis e suas permissões.", Groups.Authorization),
            new(Permissions.Authorization.RolesCreate, "Criar papéis", "Permite criar papéis personalizados.", Groups.Authorization, Permissions.Authorization.RolesView),
            new(Permissions.Authorization.RolesEdit, "Editar papéis", "Permite editar nome e descrição de papéis.", Groups.Authorization, Permissions.Authorization.RolesView),
            new(Permissions.Authorization.RolesDelete, "Excluir papéis", "Permite excluir papéis personalizados sem usuários vinculados.", Groups.Authorization, Permissions.Authorization.RolesView),
            new(Permissions.Authorization.RolesManagePermissions, "Gerenciar permissões de papel", "Permite alterar as permissões concedidas por um papel.", Groups.Authorization, Permissions.Authorization.RolesView),
            new(Permissions.Authorization.AuditView, "Ver auditoria de acesso", "Permite visualizar o registro de alterações de acesso.", Groups.Authorization),

            new(Permissions.System.JobsView, "Ver jobs", "Permite visualizar os jobs em execução.", Groups.System),
            new(Permissions.System.JobsManage, "Gerenciar jobs", "Permite disparar jobs manualmente.", Groups.System, Permissions.System.JobsView),
            new(Permissions.System.AuditView, "Ver auditoria global", "Permite visualizar o registro de criações, alterações e exclusões feitas pela equipe em todo o sistema.", Groups.System),
        };

        private static readonly HashSet<string> Names = All.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        public static readonly IReadOnlyList<string> AllNames = All.Select(p => p.Name).ToList();

        public static bool Exists(string name) => Names.Contains(name);
    }
}
