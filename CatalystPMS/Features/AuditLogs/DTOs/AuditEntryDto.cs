namespace CatalystPMS.Features.AuditLogs.DTOs
{
    public class AuditEntryDto
    {
        public string ActionType { get; set; } = string.Empty;
        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public DateTime LoggedAt { get; set; }
    }
}
