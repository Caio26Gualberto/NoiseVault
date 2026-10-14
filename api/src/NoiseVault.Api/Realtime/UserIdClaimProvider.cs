using Microsoft.AspNetCore.SignalR;
using NoiseVault.Domain.Constants;

namespace NoiseVault.Api.Realtime
{
    public class UserIdClaimProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection) =>
            connection.User?.FindFirst(UserClaimTypes.UserId)?.Value;
    }
}
