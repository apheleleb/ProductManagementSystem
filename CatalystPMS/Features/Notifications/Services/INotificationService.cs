using CatalystPMS.Features.Notifications.DTOs;

namespace CatalystPMS.Features.Notifications.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationResponseDto>> GetMyNotificationsAsync(string userId);
        Task<(bool Success, string? Error)> MarkAsReadAsync(int notificationId, string userId);
        Task<int> GetUnreadCountAsync(string userId);
    }

}
