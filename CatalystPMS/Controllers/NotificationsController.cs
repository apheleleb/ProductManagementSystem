using System.Security.Claims;
using CatalystPMS.Features.Notifications.DTOs;
using CatalystPMS.Features.Notifications.Services;
using CatalystPMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalystPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private string CurrentUserId => User.FindFirstValue("sub")!;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>Get all notifications for the current user.</summary>
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var notifications = await _notificationService.GetMyNotificationsAsync(CurrentUserId);
            return Ok(ApiResponse<IEnumerable<NotificationResponseDto>>.Ok(notifications));
        }

        /// <summary>Get unread notification count for the bell icon.</summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notificationService.GetUnreadCountAsync(CurrentUserId);
            return Ok(ApiResponse<object>.Ok(new { count }));
        }

        /// <summary>Mark a notification as read.</summary>
        [HttpPatch("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var (success, error) = await _notificationService.MarkAsReadAsync(id, CurrentUserId);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Notification marked as read."));
        }
    }
}