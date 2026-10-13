using Microsoft.AspNetCore.Authorization;
using Headbanger.Application.Abstractions.Authorization;
using System.Security.Claims;

namespace Headbanger.Api.Authorization
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionChecker _permissionChecker;

        public PermissionAuthorizationHandler(IPermissionChecker permissionChecker)
        {
            _permissionChecker = permissionChecker;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out var domainUserId))
                return;

            if (await _permissionChecker.IsGrantedAsync(domainUserId, requirement.Permission))
                context.Succeed(requirement);
        }
    }
}
