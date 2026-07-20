using CatalystPMS.Core.Models;

namespace CatalystPMS.Features.Products.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int productId);
    Task<Product?> GetByIdWithDetailsAsync(int productId);
    Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null);
    Task<IEnumerable<Product>> GetByUserAsync(string userId);
    Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? searchTerm, int? categoryId, int? statusId, int page, int pageSize);
    Task AddAsync(Product product);
    void Update(Product product);
    Task<int> CountByStatusAsync(int statusId);
    Task<IEnumerable<Product>> GetRecentAsync(int count);
}