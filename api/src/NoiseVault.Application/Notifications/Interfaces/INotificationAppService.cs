using NoiseVault.Application.Notifications.Dtos;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Notifications.Interfaces
{
    public interface INotificationAppService
    {
        Task<Result<PagedResultDto<NotificationDto>>> List(NotificationListQueryDto query);
        Task<Result<UnreadCountDto>> UnreadCount();
        Task<Result> MarkRead(int id);
        Task<Result> MarkAllRead();
        Task<Result> Delete(int id);

        Task Notify(int recipientUserId, NotificationInput input);
        Task NotifyMany(IReadOnlyCollection<int> recipientUserIds, NotificationInput input);
        Task MarkInvitationRead(int userId, int invitationId);
        Task MarkPartnershipRead(int userId, int partnershipId);
        Task<int> Purge();
    }
}
