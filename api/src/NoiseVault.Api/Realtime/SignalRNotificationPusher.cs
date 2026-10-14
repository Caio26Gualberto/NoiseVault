using Microsoft.AspNetCore.SignalR;
using NoiseVault.Application.Abstractions.Realtime;

namespace NoiseVault.Api.Realtime
{
    public class SignalRNotificationPusher : INotificationPusher
    {
        private readonly IHubContext<NotificationsHub> _hubContext;

        public SignalRNotificationPusher(IHubContext<NotificationsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task UnreadChanged(IReadOnlyDictionary<int, int> unreadByUser) =>
            Task.WhenAll(unreadByUser.Select(pair =>
                _hubContext.Clients
                    .User(pair.Key.ToString())
                    .SendAsync(NotificationsHub.UnreadMethod, new { unreadCount = pair.Value })));
    }
}
