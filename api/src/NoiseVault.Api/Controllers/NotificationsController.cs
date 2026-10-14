using Microsoft.AspNetCore.Mvc;
using NoiseVault.Application.Notifications.Dtos;
using NoiseVault.Application.Notifications.Interfaces;

namespace NoiseVault.Api.Controllers
{
    [Route("api/notifications")]
    public class NotificationsController : ApiBaseController
    {
        private readonly INotificationAppService _notificationAppService;

        public NotificationsController(INotificationAppService notificationAppService)
        {
            _notificationAppService = notificationAppService;
        }

        [HttpGet]
        public async Task<ActionResult> List([FromQuery] NotificationListQueryDto query)
        {
            var result = await _notificationAppService.List(query);
            return ToResponse(result);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult> UnreadCount()
        {
            var result = await _notificationAppService.UnreadCount();
            return ToResponse(result);
        }

        [HttpPut("{id:int}/read")]
        public async Task<ActionResult> MarkRead(int id)
        {
            var result = await _notificationAppService.MarkRead(id);
            return ToResponse(result);
        }

        [HttpPut("read-all")]
        public async Task<ActionResult> MarkAllRead()
        {
            var result = await _notificationAppService.MarkAllRead();
            return ToResponse(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _notificationAppService.Delete(id);
            return ToResponse(result);
        }
    }
}
