using CatalystPMS.Features.Products.DTOs;
using CatalystPMS.Shared.DTOs;

namespace CatalystPMS.Features.Products.Services
{
    public interface IProductService
    {
        Task<PagedResponseDto<ProductSummaryDto>> SearchAsync(
            string? searchTerm, int? categoryId, int? statusId, int page, int pageSize);
        Task<ProductResponseDto?> GetByIdAsync(int productId);
        Task<(bool Success, string? Error, int ProductId)> CreateAsync(
            CreateProductDto dto, string userId, IFormFile? image, bool submitForApproval);
        Task<(bool Success, string? Error)> UpdateAsync(
            int productId, UpdateProductDto dto, string userId, IFormFile? image);
        Task<(bool Success, string? Error)> SubmitForApprovalAsync(int productId, string userId);
        Task<IEnumerable<ProductSummaryDto>> GetMyDraftsAsync(string userId);
        Task<int> CountByStatusAsync(int statusId);
        Task<IEnumerable<ProductSummaryDto>> GetRecentActivityAsync(int count = 5);
    }
}
