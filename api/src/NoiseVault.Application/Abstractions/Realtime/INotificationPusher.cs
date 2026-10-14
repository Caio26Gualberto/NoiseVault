namespace NoiseVault.Application.Abstractions.Realtime
{
    public interface INotificationPusher
    {
        Task UnreadChanged(IReadOnlyDictionary<int, int> unreadByUser);
    }
}
