using CatalystPMS.Core.Enums;
using CatalystPMS.Core.Interfaces;
using CatalystPMS.Core.Models;
using CatalystPMS.Infrastructure.Data;
using CatalystPMS.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Features.Approvals.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly IUnitOfWork _uow;
        private readonly AppDbContext _context;
        private readonly IDataLakeService _dataLake;

        public ApprovalService(IUnitOfWork uow, AppDbContext context, IDataLakeService dataLake)
        {
            _uow = uow;
            _context = context;
            _dataLake = dataLake;
        }

        public async Task<(bool Success, string? Error)> ApproveAsync(
            int productId, string managerId, string? comment)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) return (false, "Product not found.");

            if (product.StatusId != (int)ProductStatusEnum.PendingApproval)
                return (false, "Only products with Pending Approval status can be approved.");

            product.StatusId = (int)ProductStatusEnum.Approved;
            product.ApprovedByUserId = managerId;
            product.UpdatedAt = DateTime.UtcNow;

            _uow.Products.Update(product);
            await WriteAuditAsync(productId, managerId, "StatusChanged", "Status", "Pending Approval", "Approved");
            await WriteWorkflowAsync(productId, managerId, "Approved", comment);
            await WriteNotificationAsync(productId, product.CreatedByUserId,
                $"Your product '{product.Name}' has been approved.");

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RejectAsync(
            int productId, string managerId, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return (false, "A rejection reason is required.");

            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) return (false, "Product not found.");

            if (product.StatusId != (int)ProductStatusEnum.PendingApproval)
                return (false, "Only products with Pending Approval status can be rejected.");

            product.StatusId = (int)ProductStatusEnum.Rejected;
            product.UpdatedAt = DateTime.UtcNow;

            _uow.Products.Update(product);
            await WriteAuditAsync(productId, managerId, "StatusChanged", "Status", "Pending Approval", "Rejected");
            await WriteWorkflowAsync(productId, managerId, "Rejected", comment);
            await WriteNotificationAsync(productId, product.CreatedByUserId,
                $"Your product '{product.Name}' was rejected. Reason: {comment}");

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> PublishAsync(int productId, string managerId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) return (false, "Product not found.");

            if (product.StatusId != (int)ProductStatusEnum.Approved)
                return (false, "Only Approved products can be published.");

            product.StatusId = (int)ProductStatusEnum.Active;
            product.UpdatedAt = DateTime.UtcNow;

            _uow.Products.Update(product);
            await WriteAuditAsync(productId, managerId, "StatusChanged", "Status", "Approved", "Active");
            await WriteWorkflowAsync(productId, managerId, "Published", null);
            await WriteNotificationAsync(productId, product.CreatedByUserId,
                $"Your product '{product.Name}' is now live.");

            await _context.SaveChangesAsync();

            // Trigger Data Lake sync after publish
            await _dataLake.SyncProductAsync(product);

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UnpublishAsync(int productId, string managerId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) return (false, "Product not found.");

            if (product.StatusId != (int)ProductStatusEnum.Active)
                return (false, "Only Active (published) products can be unpublished.");

            product.StatusId = (int)ProductStatusEnum.Inactive;
            product.UpdatedAt = DateTime.UtcNow;

            _uow.Products.Update(product);
            await WriteAuditAsync(productId, managerId, "StatusChanged", "Status", "Active", "Inactive");
            await WriteWorkflowAsync(productId, managerId, "Unpublished", null);

            await _context.SaveChangesAsync();

            // Product is no longer live — remove its snapshot from the Data Lake.
            await _dataLake.RemoveProductAsync(productId);

            return (true, null);
        }

        // Status IDs — match your ProductStatus seed data / DbSeeder:
        // 1 = Draft, 2 = Pending Approval, 3 = Approved, 4 = Rejected, 5 = Published, 6 = Archived
        private const int StatusApproved = 3;
        private const int StatusArchived = 6;

        public async Task<(bool Success, string? Error)> ArchiveAsync(int productId, string actorUserId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
                return (false, "Product not found.");

            if (product.IsDeleted)
                return (false, "This product has already been archived.");

            var previousStatusName = product.Status?.StatusName ?? product.StatusId.ToString();
            var now = DateTime.UtcNow;

            product.IsDeleted = true;
            product.DeletedAt = now;
            product.DeletedByUserId = actorUserId;
            product.StatusId = StatusArchived;
            product.UpdatedAt = now;

            _context.ApprovalWorkflows.Add(new ApprovalWorkflow
            {
                ProductId = productId,
                Action = "Archived",
                ActorUserId = actorUserId,
                ActionDate = now
            });

            _context.AuditLogs.Add(new AuditLog
            {
                ProductId = productId,
                ActionType = "Archived",
                FieldName = "IsDeleted",
                OldValue = "false",
                NewValue = "true",
                ActorUserId = actorUserId,
                LoggedAt = now
            });

            _context.Notifications.Add(new Notification
            {
                ProductId = productId,
                RecipientUserId = product.CreatedByUserId,
                Message = $"Your product '{product.Name}' was archived by a manager.",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();

            // Archived products are no longer live — remove from the Data Lake too.
            await _dataLake.RemoveProductAsync(productId);

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RestoreAsync(int productId, string actorUserId)
        {
            var product = await _context.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return (false, "Product not found.");

            if (!product.IsDeleted)
                return (false, "This product is not archived.");

            var now = DateTime.UtcNow;

            product.IsDeleted = false;
            product.DeletedAt = null;
            product.DeletedByUserId = null;
            product.StatusId = StatusApproved; // Manager can re-publish from here if needed
            product.UpdatedAt = now;

            _context.ApprovalWorkflows.Add(new ApprovalWorkflow
            {
                ProductId = productId,
                Action = "Restored",
                ActorUserId = actorUserId,
                ActionDate = now
            });

            _context.AuditLogs.Add(new AuditLog
            {
                ProductId = productId,
                ActionType = "Restored",
                FieldName = "IsDeleted",
                OldValue = "true",
                NewValue = "false",
                ActorUserId = actorUserId,
                LoggedAt = now
            });

            _context.Notifications.Add(new Notification
            {
                ProductId = productId,
                RecipientUserId = product.CreatedByUserId,
                Message = $"Your product '{product.Name}' was restored from the archive.",
                CreatedAt = now
            });

            await _context.SaveChangesAsync();
            return (true, null);
        }

        private async Task WriteAuditAsync(int productId, string actorId, string actionType,
            string? fieldName, string? oldValue, string? newValue)
        {
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                ProductId = productId,
                ActorUserId = actorId,
                ActionType = actionType,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                LoggedAt = DateTime.UtcNow
            });
        }

        private async Task WriteWorkflowAsync(int productId, string actorId, string action, string? comment)
        {
            await _context.ApprovalWorkflows.AddAsync(new ApprovalWorkflow
            {
                ProductId = productId,
                ActorUserId = actorId,
                Action = action,
                Comment = comment,
                ActionDate = DateTime.UtcNow
            });
        }

        private async Task WriteNotificationAsync(int productId, string recipientId, string message)
        {
            await _context.Notifications.AddAsync(new Notification
            {
                ProductId = productId,
                RecipientUserId = recipientId,
                Message = message,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}

//using CatalystPMS.Core.Enums;
//using CatalystPMS.Core.Interfaces;
//using CatalystPMS.Core.Models;
//using CatalystPMS.Infrastructure.Data;
//using CatalystPMS.Infrastructure.ExternalServices;
//using Microsoft.EntityFrameworkCore;

//namespace CatalystPMS.Features.Approvals.Services
//{
//    public class ApprovalService : IApprovalService
//    {
//        private readonly IUnitOfWork _uow;
//        private readonly AppDbContext _context;
//        private readonly IDataLakeService _dataLake;

//        public ApprovalService(IUnitOfWork uow, AppDbContext context, IDataLakeService dataLake)
//        {
//            _uow = uow;
//            _context = context;
//            _dataLake = dataLake;
//        }

//        public async Task<(bool Success, string? Error)> ApproveAsync(
//            int productId, string managerId, string? comment)
//        {
//            var product = await _uow.Products.GetByIdAsync(productId);
//            if (product == null) return (false, "Product not found.");

//            if (product.StatusId != (int)ProductStatusEnum.PendingApproval)
//                return (false, "Only products with Pending Approval status can be approved.");

//            product.StatusId = (int)ProductStatusEnum.Approved;
//            product.ApprovedByUserId = managerId;
//            product.UpdatedAt = DateTime.UtcNow;

//            _uow.Products.Update(product);
//            await WriteAuditAsync(productId, managerId, "StatusChanged", "Status", "Pending Approval", "Approved");
//            await WriteWorkflowAsync(productId, managerId, "Approved", comment);
//            await WriteNotificationAsync(productId, product.CreatedByUserId,
//                $"Your product '{product.Name}' has been approved.");

//            await _context.SaveChangesAsync();
//            return (true, null);
//        }

//        public async Task<(bool Success, string? Error)> RejectAsync(
//            int productId, string managerId, string comment)
//        {
//            if (string.IsNullOrWhiteSpace(comment))
//                return (false, "A rejection reason is required.");

//            var product = await _uow.Products.GetByIdAsync(productId);
//            if (product == null) return (false, "Product not found.");

//            if (product.StatusId != (int)ProductStatusEnum.PendingApproval)
//                return (false, "Only products with Pending Approval status can be rejected.");

//            product.StatusId = (int)ProductStatusEnum.Rejected;
//            product.UpdatedAt = DateTime.UtcNow;

//            _uow.Products.Update(product);
//            await WriteAuditAsync(productId, managerId, "StatusChanged", "Status", "Pending Approval", "Rejected");
//            await WriteWorkflowAsync(productId, managerId, "Rejected", comment);
//            await WriteNotificationAsync(productId, product.CreatedByUserId,
//                $"Your product '{product.Name}' was rejected. Reason: {comment}");

//            await _context.SaveChangesAsync();
//            return (true, null);
//        }

//        public async Task<(bool Success, string? Error)> PublishAsync(int productId, string managerId)
//        {
//            var product = await _uow.Products.GetByIdAsync(productId);
//            if (product == null) return (false, "Product not found.");

//            if (product.StatusId != (int)ProductStatusEnum.Approved)
//                return (false, "Only Approved products can be published.");

//            product.StatusId = (int)ProductStatusEnum.Active;
//            product.UpdatedAt = DateTime.UtcNow;

//            _uow.Products.Update(product);
//            await WriteAuditAsync(productId, managerId, "StatusChanged", "Status", "Approved", "Active");
//            await WriteWorkflowAsync(productId, managerId, "Published", null);
//            await WriteNotificationAsync(productId, product.CreatedByUserId,
//                $"Your product '{product.Name}' is now live.");

//            await _context.SaveChangesAsync();

//            // Trigger Data Lake sync after publish
//            await _dataLake.SyncProductAsync(product);

//            return (true, null);
//        }

//        public async Task<(bool Success, string? Error)> UnpublishAsync(int productId, string managerId)
//        {
//            var product = await _uow.Products.GetByIdAsync(productId);
//            if (product == null) return (false, "Product not found.");

//            if (product.StatusId != (int)ProductStatusEnum.Active)
//                return (false, "Only Active (published) products can be unpublished.");

//            product.StatusId = (int)ProductStatusEnum.Inactive;
//            product.UpdatedAt = DateTime.UtcNow;

//            _uow.Products.Update(product);
//            await WriteAuditAsync(productId, managerId, "StatusChanged", "Status", "Active", "Inactive");
//            await WriteWorkflowAsync(productId, managerId, "Unpublished", null);

//            await _context.SaveChangesAsync();
//            return (true, null);
//        }

//        //public async Task<(bool Success, string? Error)> ArchiveAsync(int productId, string managerId)
//        //{
//        //    var product = await _uow.Products.GetByIdAsync(productId);
//        //    if (product == null) return (false, "Product not found.");

//        //    if (product.StatusId == (int)ProductStatusEnum.Archived)
//        //        return (false, "Product is already archived.");

//        //    var oldStatus = product.Status?.StatusName ?? product.StatusId.ToString();
//        //    product.StatusId = (int)ProductStatusEnum.Archived;
//        //    product.UpdatedAt = DateTime.UtcNow;

//        //    _uow.Products.Update(product);
//        //    await WriteAuditAsync(productId, managerId, "Archived", "Status", oldStatus, "Archived");
//        //    await WriteWorkflowAsync(productId, managerId, "Archived", null);

//        //    await _context.SaveChangesAsync();
//        //    return (true, null);
//        //}



//        // Status IDs — match your ProductStatus seed data / DbSeeder:
//        // 1 = Draft, 2 = Pending Approval, 3 = Approved, 4 = Rejected, 5 = Published, 6 = Archived
//        private const int StatusApproved = 3;
//        private const int StatusArchived = 6;

//        public async Task<(bool Success, string? Error)> ArchiveAsync(int productId, string actorUserId)
//        {
//            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
//            if (product == null)
//                return (false, "Product not found.");

//            if (product.IsDeleted)
//                return (false, "This product has already been archived.");

//            var previousStatusName = product.Status?.StatusName ?? product.StatusId.ToString();
//            var now = DateTime.UtcNow;

//            product.IsDeleted = true;
//            product.DeletedAt = now;
//            product.DeletedByUserId = actorUserId;
//            product.StatusId = StatusArchived;
//            product.UpdatedAt = now;

//            _context.ApprovalWorkflows.Add(new ApprovalWorkflow
//            {
//                ProductId = productId,
//                Action = "Archived",
//                ActorUserId = actorUserId,
//                ActionDate = now
//            });

//            _context.AuditLogs.Add(new AuditLog
//            {
//                ProductId = productId,
//                ActionType = "Archived",
//                FieldName = "IsDeleted",
//                OldValue = "false",
//                NewValue = "true",
//                ActorUserId = actorUserId,
//                LoggedAt = now
//            });

//            _context.Notifications.Add(new Notification
//            {
//                ProductId = productId,
//                RecipientUserId = product.CreatedByUserId,
//                Message = $"Your product '{product.Name}' was archived by a manager.",
//                CreatedAt = now
//            });

//            await _context.SaveChangesAsync();
//            return (true, null);
//        }

//        public async Task<(bool Success, string? Error)> RestoreAsync(int productId, string actorUserId)
//        {
//            // IgnoreQueryFilters() is required here — a soft-deleted product is invisible
//            // to the default query, so without this, RestoreAsync could never find it.
//            var product = await _context.Products
//                .IgnoreQueryFilters()
//                .FirstOrDefaultAsync(p => p.ProductId == productId);

//            if (product == null)
//                return (false, "Product not found.");

//            if (!product.IsDeleted)
//                return (false, "This product is not archived.");

//            var now = DateTime.UtcNow;

//            product.IsDeleted = false;
//            product.DeletedAt = null;
//            product.DeletedByUserId = null;
//            product.StatusId = StatusApproved; // Manager can re-publish from here if needed
//            product.UpdatedAt = now;

//            _context.ApprovalWorkflows.Add(new ApprovalWorkflow
//            {
//                ProductId = productId,
//                Action = "Restored",
//                ActorUserId = actorUserId,
//                ActionDate = now
//            });

//            _context.AuditLogs.Add(new AuditLog
//            {
//                ProductId = productId,
//                ActionType = "Restored",
//                FieldName = "IsDeleted",
//                OldValue = "true",
//                NewValue = "false",
//                ActorUserId = actorUserId,
//                LoggedAt = now
//            });

//            _context.Notifications.Add(new Notification
//            {
//                ProductId = productId,
//                RecipientUserId = product.CreatedByUserId,
//                Message = $"Your product '{product.Name}' was restored from the archive.",
//                CreatedAt = now
//            });

//            await _context.SaveChangesAsync();
//            return (true, null);
//        }

//        private async Task WriteAuditAsync(int productId, string actorId, string actionType,
//            string? fieldName, string? oldValue, string? newValue)
//        {
//            await _context.AuditLogs.AddAsync(new AuditLog
//            {
//                ProductId = productId,
//                ActorUserId = actorId,
//                ActionType = actionType,
//                FieldName = fieldName,
//                OldValue = oldValue,
//                NewValue = newValue,
//                LoggedAt = DateTime.UtcNow
//            });
//        }

//        private async Task WriteWorkflowAsync(int productId, string actorId, string action, string? comment)
//        {
//            await _context.ApprovalWorkflows.AddAsync(new ApprovalWorkflow
//            {
//                ProductId = productId,
//                ActorUserId = actorId,
//                Action = action,
//                Comment = comment,
//                ActionDate = DateTime.UtcNow
//            });
//        }

//        private async Task WriteNotificationAsync(int productId, string recipientId, string message)
//        {
//            await _context.Notifications.AddAsync(new Notification
//            {
//                ProductId = productId,
//                RecipientUserId = recipientId,
//                Message = message,
//                CreatedAt = DateTime.UtcNow
//            });
//        }
//    }
//}
