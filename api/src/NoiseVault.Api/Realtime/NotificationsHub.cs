using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NoiseVault.Api.Realtime
{
    [Authorize]
    public class NotificationsHub : Hub
    {
        public const string Path = "/hubs/notifications";
        public const string UnreadMethod = "unread";
    }
}
