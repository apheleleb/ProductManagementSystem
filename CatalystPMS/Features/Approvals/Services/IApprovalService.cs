namespace CatalystPMS.Features.Approvals.Services
{
    public interface IApprovalService
    {
        Task<(bool Success, string? Error)> ApproveAsync(int productId, string managerId, string? comment);
        Task<(bool Success, string? Error)> RejectAsync(int productId, string managerId, string comment);
        Task<(bool Success, string? Error)> PublishAsync(int productId, string managerId);
        Task<(bool Success, string? Error)> UnpublishAsync(int productId, string managerId);

        /// <summary>
        /// Soft-deletes the product: sets IsDeleted/DeletedAt/DeletedByUserId and moves
        /// StatusId to Archived. The product is immediately hidden from all normal
        /// queries via the global EF Core query filter on Product.
        /// </summary>
        Task<(bool Success, string? Error)> ArchiveAsync(int productId, string actorUserId);

        /// <summary>
        /// Reverses a soft delete: clears IsDeleted/DeletedAt/DeletedByUserId and moves
        /// the product back to Approved status. Manager-only, called from the
        /// "recycle bin" view.
        /// </summary>
        Task<(bool Success, string? Error)> RestoreAsync(int productId, string actorUserId);
    }
}
