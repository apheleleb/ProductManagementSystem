namespace CatalystPMS.Features.Approvals.DTOs
{
    public class ApprovalHistoryDto
    {
        public string Action { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
    }
}
