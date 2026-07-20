namespace CatalystPMS.Features.Notifications.DTOs
{
    public class NotificationResponseDto
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public int ProductId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
