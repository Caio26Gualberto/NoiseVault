using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Headbanger.Api.ApiResponse;
using Headbanger.Domain.Constants;

namespace Headbanger.Api.Authorization
{
    public class PasswordChangeRequiredFilter : IAsyncAuthorizationFilter
    {
        private static readonly string[] AllowedPaths =
        [
            "/api/profile",
            "/api/profile/change-password",
            "/api/profile/set-initial-password",
            "/api/admin/authorization/me",
            "/api/auth/logout",
            "/api/auth/refresh-token",
        ];

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user?.Identity?.IsAuthenticated != true)
                return Task.CompletedTask;

            if (user.FindFirst(UserClaimTypes.PasswordChangeRequired) is null)
                return Task.CompletedTask;

            var path = context.HttpContext.Request.Path.Value ?? string.Empty;

            if (AllowedPaths.Any(allowed => path.Equals(allowed, StringComparison.OrdinalIgnoreCase)))
                return Task.CompletedTask;

            context.Result = new ObjectResult(new ApiResponse<object>
            {
                IsSuccess = false,
                Code = "PASSWORD_CHANGE_REQUIRED",
                Message = "You must change your password before continuing",
                Data = null
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

            return Task.CompletedTask;
        }
    }
}
