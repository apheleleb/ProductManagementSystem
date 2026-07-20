using CatalystPMS.Features.Notifications.DTOs;
using CatalystPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Features.Notifications.Services
{

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context) => _context = context;

        public async Task<IEnumerable<NotificationResponseDto>> GetMyNotificationsAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications.Select(n => new NotificationResponseDto
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                IsRead = n.IsRead,
                ProductId = n.ProductId,
                CreatedAt = n.CreatedAt
            });
        }

        public async Task<(bool Success, string? Error)> MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return (false, "Notification not found.");
            if (notification.RecipientUserId != userId) return (false, "Access denied.");

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<int> GetUnreadCountAsync(string userId) =>
            await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
    }
}
