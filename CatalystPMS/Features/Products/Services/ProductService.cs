using CatalystPMS.Core.Enums;
using CatalystPMS.Core.Interfaces;
using CatalystPMS.Core.Models;
using CatalystPMS.Features.Approvals.DTOs;
using CatalystPMS.Features.AuditLogs.DTOs;
using CatalystPMS.Features.Products.DTOs;
using CatalystPMS.Infrastructure.Data;
using CatalystPMS.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Features.Products.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        private readonly AppDbContext _context;

        public ProductService(IUnitOfWork uow, AppDbContext context)
        {
            _uow = uow;
            _context = context;
        }

        public async Task<PagedResponseDto<ProductSummaryDto>> SearchAsync(
            string? searchTerm, int? categoryId, int? statusId, int page, int pageSize)
        {
            var (items, totalCount) = await _uow.Products.SearchAsync(
                searchTerm, categoryId, statusId, page, pageSize);

            return new PagedResponseDto<ProductSummaryDto>
            {
                Items = items.Select(MapToSummary),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int productId)
        {
            var product = await _uow.Products.GetByIdWithDetailsAsync(productId);
            return product == null ? null : MapToDetail(product);
        }

        public async Task<(bool Success, string? Error, int ProductId)> CreateAsync(
            CreateProductDto dto, string userId, IFormFile? image, bool submitForApproval)
        {
            if (await _uow.Products.SkuExistsAsync(dto.Sku))
                return (false, $"SKU '{dto.Sku}' already exists.", 0);

            var statusId = submitForApproval
                ? (int)ProductStatusEnum.PendingApproval
                : (int)ProductStatusEnum.Draft;

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Sku = dto.Sku,
                Brand = dto.Brand,
                UnitPrice = dto.UnitPrice,
                CategoryId = dto.CategoryId,
                StatusId = statusId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Specifications = dto.Specifications.Select(s => new ProductSpecification
                {
                    Key = s.Key,
                    Value = s.Value
                }).ToList()
            };

            if (image != null)
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                product.ImageData = ms.ToArray();
                product.ImageMimeType = image.ContentType;
            }

            await _uow.Products.AddAsync(product);
            await _uow.SaveChangesAsync(); // Save first to get ProductId

            await WriteAuditAsync(product.ProductId, userId, "Created", null, null, product.Name);

            if (submitForApproval)
            {
                await WriteAuditAsync(product.ProductId, userId, "StatusChanged", "Status", "Draft", "Pending Approval");
                await WriteWorkflowAsync(product.ProductId, userId, "Submitted", null);
            }

            await _context.SaveChangesAsync();
            return (true, null, product.ProductId);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(
            int productId, UpdateProductDto dto, string userId, IFormFile? image)
        {
            var product = await _uow.Products.GetByIdWithDetailsAsync(productId);
            if (product == null) return (false, "Product not found.");

            var editableStatuses = new[]
            {
            (int)ProductStatusEnum.Draft,
            (int)ProductStatusEnum.PendingApproval,
            (int)ProductStatusEnum.Rejected
        };

            if (!editableStatuses.Contains(product.StatusId))
                return (false, "Only Draft, Pending Approval, or Rejected products can be edited.");

            var changes = new List<(string Field, string Old, string New)>();
            if (product.Name != dto.Name) changes.Add(("Name", product.Name, dto.Name));
            if (product.Description != dto.Description) changes.Add(("Description", product.Description, dto.Description));
            if (product.Brand != dto.Brand) changes.Add(("Brand", product.Brand, dto.Brand));
            if (product.UnitPrice != dto.UnitPrice) changes.Add(("UnitPrice", product.UnitPrice.ToString(), dto.UnitPrice.ToString()));
            if (product.CategoryId != dto.CategoryId) changes.Add(("CategoryId", product.CategoryId.ToString(), dto.CategoryId.ToString()));

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Brand = dto.Brand;
            product.UnitPrice = dto.UnitPrice;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            product.Specifications.Clear();
            foreach (var spec in dto.Specifications)
                product.Specifications.Add(new ProductSpecification { Key = spec.Key, Value = spec.Value });

            if (image != null)
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                product.ImageData = ms.ToArray();
                product.ImageMimeType = image.ContentType;
                changes.Add(("Image", "previous", "updated"));
            }

            if (product.StatusId == (int)ProductStatusEnum.Rejected)
            {
                changes.Add(("Status", "Rejected", "Draft"));
                product.StatusId = (int)ProductStatusEnum.Draft;
            }

            _uow.Products.Update(product);

            foreach (var (field, old, newVal) in changes)
                await WriteAuditAsync(product.ProductId, userId, "Updated", field, old, newVal);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> SubmitForApprovalAsync(int productId, string userId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) return (false, "Product not found.");

            if (product.CreatedByUserId != userId)
                return (false, "You can only submit your own products.");

            var submittableStatuses = new[]
            {
            (int)ProductStatusEnum.Draft,
            (int)ProductStatusEnum.Rejected
        };

            if (!submittableStatuses.Contains(product.StatusId))
                return (false, "Only Draft or Rejected products can be submitted for approval.");

            var oldStatus = product.StatusId == (int)ProductStatusEnum.Draft ? "Draft" : "Rejected";
            product.StatusId = (int)ProductStatusEnum.PendingApproval;
            product.UpdatedAt = DateTime.UtcNow;

            _uow.Products.Update(product);
            await WriteAuditAsync(product.ProductId, userId, "StatusChanged", "Status", oldStatus, "Pending Approval");
            await WriteWorkflowAsync(product.ProductId, userId, "Submitted", null);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<IEnumerable<ProductSummaryDto>> GetMyDraftsAsync(string userId)
        {
            var products = await _uow.Products.GetByUserAsync(userId);
            return products
                .Where(p => p.StatusId == (int)ProductStatusEnum.Draft)
                .Select(MapToSummary);
        }

        //public async Task<int> CountByStatusAsync(int statusId) =>
        //    await _uow.Products.CountByStatusAsync(statusId);
        public async Task<int> CountByStatusAsync(int statusId)
        {
            return await _context.Products
                .IgnoreQueryFilters()
                .CountAsync(p => p.StatusId == statusId);
        }

        public async Task<IEnumerable<ProductSummaryDto>> GetRecentActivityAsync(int count = 5)
        {
            var recent = await _uow.Products.GetRecentAsync(count);
            return recent.Select(MapToSummary);
        }

        // ── Private helpers ────────────────────────────────────────────────────────

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

        private static ProductSummaryDto MapToSummary(Product p) => new()
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Sku = p.Sku,
            Brand = p.Brand,
            CategoryName = p.Category?.Name ?? string.Empty,
            StatusName = p.Status?.StatusName ?? string.Empty,
            UnitPrice = p.UnitPrice,
            UpdatedAt = p.UpdatedAt,
            CreatedByUserId = p.CreatedByUserId
        };

        private static ProductResponseDto MapToDetail(Product p) => new()
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Description = p.Description,
            Sku = p.Sku,
            Brand = p.Brand,
            UnitPrice = p.UnitPrice,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            StatusId = p.StatusId,
            StatusName = p.Status?.StatusName ?? string.Empty,
            CreatedByUserId = p.CreatedByUserId,
            ApprovedByUserId = p.ApprovedByUserId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            HasImage = p.ImageData != null,
            Specifications = p.Specifications.Select(s => new ProductSpecificationDto
            { Key = s.Key, Value = s.Value }).ToList(),
            ApprovalHistory = p.ApprovalWorkflows.Select(a => new ApprovalHistoryDto
            {
                Action = a.Action,
                Comment = a.Comment,
                ActorUserId = a.ActorUserId,
                ActionDate = a.ActionDate
            }).ToList(),
            AuditLog = p.AuditLogs.Select(a => new AuditEntryDto
            {
                ActionType = a.ActionType,
                FieldName = a.FieldName,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                ActorUserId = a.ActorUserId,
                LoggedAt = a.LoggedAt
            }).ToList()
        };
    }
}
