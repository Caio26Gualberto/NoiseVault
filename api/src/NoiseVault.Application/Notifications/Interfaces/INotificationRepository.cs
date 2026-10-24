using NoiseVault.Application.Notifications.Models;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.Notifications.Interfaces
{
    public interface INotificationRepository
    {
        Task<List<int>> GroupAsync(IReadOnlyCollection<int> userIds, NotificationInput input, DateTime now);
        Task AddRangeAsync(IReadOnlyCollection<Notification> notifications);
        Task<(List<NotificationReadModel> Items, int TotalCount)> PageAsync(int userId, bool unreadOnly, int skip, int take);
        Task<Dictionary<int, int>> UnreadCountsAsync(IReadOnlyCollection<int> userIds);
        Task<bool> MarkReadAsync(int userId, int notificationId, DateTime now);
        Task<int> MarkAllReadAsync(int userId, DateTime now);
        Task<int> MarkInvitationReadAsync(int userId, int invitationId, DateTime now);
        Task<int> MarkPartnershipReadAsync(int userId, int partnershipId, DateTime now);
        Task<bool> DeleteAsync(int userId, int notificationId);
        Task<int> PurgeAsync(DateTime olderThan);
    }
}
