namespace CatalystPMS.Core.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Plain string FKs to ApplicationUser (Identity uses string PKs)
        public string CreatedByUserId { get; set; } = string.Empty;
        public string? ApprovedByUserId { get; set; }

        // ── Soft delete ──────────────────────────────────────────────────
        // A product is soft-deleted via the Manager's "Archive" workflow action.
        // IsDeleted is enforced by a global EF Core query filter (see AppDbContext),
        // so it is automatically excluded from every normal query. Managers can
        // still see and restore deleted products via the dedicated "deleted" 
        // endpoints, which explicitly bypass the filter.
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int StatusId { get; set; }
        public ProductStatus Status { get; set; } = null!;

        public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
        public ICollection<ApprovalWorkflow> ApprovalWorkflows { get; set; } = new List<ApprovalWorkflow>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
