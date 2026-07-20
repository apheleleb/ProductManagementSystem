namespace CatalystPMS.Core.Models
{
    public class AuditLog
    {
        public int LogId { get; set; }

        // ActionType values: "Created", "Updated", "StatusChanged", "Archived"
        public string ActionType { get; set; } = string.Empty;
        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

        public string ActorUserId { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
