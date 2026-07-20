namespace CatalystPMS.Core.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string RecipientUserId { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
