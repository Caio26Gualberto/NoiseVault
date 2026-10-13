using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Headbanger.Application.Authorizations.Dtos;
using Headbanger.Application.Authorizations.Interfaces;
using Headbanger.Domain.Constants;

namespace Headbanger.Api.Controllers.Admin
{
    [Route("api/admin/authorization")]
    public class AuthorizationController : ApiBaseController
    {
        private readonly IAuthorizationAppService _appService;

        public AuthorizationController(IAuthorizationAppService appService)
        {
            _appService = appService;
        }

        [HttpGet("permissions")]
        [Authorize(Policy = Permissions.Authorization.RolesView)]
        public async Task<ActionResult> GetPermissions()
        {
            var result = await _appService.GetPermissionCatalog();
            return ToResponse(result);
        }

        [HttpGet("roles")]
        [Authorize(Policy = Permissions.Authorization.RolesView)]
        public async Task<ActionResult> ListRoles()
        {
            var result = await _appService.ListRoles();
            return ToResponse(result);
        }

        [HttpGet("roles/{roleId:int}")]
        [Authorize(Policy = Permissions.Authorization.RolesView)]
        public async Task<ActionResult> GetRole(int roleId)
        {
            var result = await _appService.GetRole(roleId);
            return ToResponse(result);
        }

        [HttpPost("roles")]
        [Authorize(Policy = Permissions.Authorization.RolesCreate)]
        public async Task<ActionResult> CreateRole([FromBody] CreateRoleRequestDto request)
        {
            var result = await _appService.CreateRole(request);
            return ToResponse(result);
        }

        [HttpPut("roles/{roleId:int}")]
        [Authorize(Policy = Permissions.Authorization.RolesEdit)]
        public async Task<ActionResult> UpdateRole(int roleId, [FromBody] UpdateRoleRequestDto request)
        {
            var result = await _appService.UpdateRole(roleId, request);
            return ToResponse(result);
        }

        [HttpDelete("roles/{roleId:int}")]
        [Authorize(Policy = Permissions.Authorization.RolesDelete)]
        public async Task<ActionResult> DeleteRole(int roleId)
        {
            var result = await _appService.DeleteRole(roleId);
            return ToResponse(result);
        }

        [HttpGet("roles/{roleId:int}/permissions")]
        [Authorize(Policy = Permissions.Authorization.RolesView)]
        public async Task<ActionResult> GetRolePermissions(int roleId)
        {
            var result = await _appService.GetRolePermissions(roleId);
            return ToResponse(result);
        }

        [HttpPut("roles/{roleId:int}/permissions")]
        [Authorize(Policy = Permissions.Authorization.RolesManagePermissions)]
        public async Task<ActionResult> SetRolePermissions(int roleId, [FromBody] RolePermissionsRequestDto request)
        {
            var result = await _appService.SetRolePermissions(roleId, request);
            return ToResponse(result);
        }

        [HttpGet("users")]
        [Authorize(Policy = Permissions.Authorization.UsersView)]
        public async Task<ActionResult> ListUsers()
        {
            var result = await _appService.ListUsers();
            return ToResponse(result);
        }

        [HttpGet("users/{userId:int}/access")]
        [Authorize(Policy = Permissions.Authorization.UsersView)]
        public async Task<ActionResult> GetUserAccess(int userId)
        {
            var result = await _appService.GetUserAccess(userId);
            return ToResponse(result);
        }

        [HttpGet("users/{userId:int}/role-preview")]
        [Authorize(Policy = Permissions.Authorization.UsersView)]
        public async Task<ActionResult> GetRoleChangePreview(int userId, [FromQuery] int roleId)
        {
            var result = await _appService.GetRoleChangePreview(userId, roleId);
            return ToResponse(result);
        }

        [HttpPut("users/{userId:int}/role")]
        [Authorize(Policy = Permissions.Authorization.UsersAssignRole)]
        public async Task<ActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequestDto request)
        {
            var result = await _appService.UpdateUserRole(userId, request);
            return ToResponse(result);
        }

        [HttpPut("users/{userId:int}/permissions")]
        [Authorize(Policy = Permissions.Authorization.UsersManagePermissions)]
        public async Task<ActionResult> UpdateUserPermissions(int userId, [FromBody] UpdateUserPermissionsRequestDto request)
        {
            var result = await _appService.UpdateUserPermissions(userId, request);
            return ToResponse(result);
        }

        [HttpGet("me")]
        public async Task<ActionResult> Me()
        {
            var result = await _appService.Me();
            return ToResponse(result);
        }

        [HttpGet("audit-logs")]
        [Authorize(Policy = Permissions.Authorization.AuditView)]
        public async Task<ActionResult> ListAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _appService.ListAuditLogs(page, pageSize);
            return ToResponse(result);
        }
    }
}
