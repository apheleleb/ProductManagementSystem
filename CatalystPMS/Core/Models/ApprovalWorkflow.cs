namespace CatalystPMS.Core.Models
{
    public class ApprovalWorkflow
    {
        public int WorkflowId { get; set; }

        // Action values: "Submitted", "Approved", "Rejected", "Published", "Unpublished", "Archived"
        public string Action { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        public string ActorUserId { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
