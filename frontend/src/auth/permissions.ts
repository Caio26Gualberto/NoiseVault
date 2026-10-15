export const Permissions = {
  adminAccess: 'Admin.Access',

  authorizationUsersView: 'Authorization.Users.View',
  authorizationUsersInvite: 'Authorization.Users.Invite',
  authorizationUsersDelete: 'Authorization.Users.Delete',
  authorizationUsersAssignRole: 'Authorization.Users.AssignRole',
  authorizationUsersManagePermissions: 'Authorization.Users.ManagePermissions',
  authorizationRolesView: 'Authorization.Roles.View',
  authorizationRolesCreate: 'Authorization.Roles.Create',
  authorizationRolesEdit: 'Authorization.Roles.Edit',
  authorizationRolesDelete: 'Authorization.Roles.Delete',
  authorizationRolesManagePermissions: 'Authorization.Roles.ManagePermissions',
  authorizationAuditView: 'Authorization.Audit.View',

  archiveCreate: 'Archive.Create',
  archiveEdit: 'Archive.Edit',
  archiveDelete: 'Archive.Delete',
  archiveReview: 'Archive.Review',

  bandsVerificationsView: 'Bands.Verifications.View',
  bandsVerify: 'Bands.Verify',

  supportView: 'Support.View',
  supportEdit: 'Support.Edit',
  supportReply: 'Support.Reply',

  systemJobsView: 'System.Jobs.View',
  systemJobsManage: 'System.Jobs.Manage',
  systemAuditView: 'System.Audit.View',
} as const

export type Permission = (typeof Permissions)[keyof typeof Permissions]
