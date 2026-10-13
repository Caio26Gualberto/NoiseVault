using Headbanger.Application.Abstractions.Authentication;
using Headbanger.Domain.Constants;
using System.Security.Claims;

namespace Headbanger.Api.Authentication
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal User =>
            _httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("Não existe um contexto de usuário.");

        public int? UserIdOrNull
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;

                var value = user?.FindFirstValue(UserClaimTypes.UserId)
                    ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

                return int.TryParse(value, out var id) ? id : null;
            }
        }

        public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        public int UserId
        {
            get
            {
                var value = User.FindFirstValue(UserClaimTypes.UserId)
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

                return int.TryParse(value, out var id)
                    ? id
                    : throw new UnauthorizedAccessException("O token não contém um ID de usuário válido.");
            }
        }

        public string? Role =>
            _httpContextAccessor.HttpContext?.User?.FindFirstValue("role")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

        public string Email =>
            User.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("O token não contém o e-mail.");

        public string Name =>
            User.FindFirstValue(ClaimTypes.Name)
            ?? throw new UnauthorizedAccessException("O token não contém o nome.");

        public string? IpAddress
        {
            get
            {
                var context = _httpContextAccessor.HttpContext;
                if (context is null)
                    return null;

                var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(forwarded))
                    return forwarded.Split(',')[0].Trim();

                return context.Connection.RemoteIpAddress?.ToString();
            }
        }
    }
}
