using NoiseVault.Application.Pagination;

namespace NoiseVault.Application.Notifications.Dtos
{
    public class NotificationListQueryDto : PagedRequestDto
    {
        public bool UnreadOnly { get; set; }
    }
}
