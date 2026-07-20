using CatalystPMS.Core.Models;
using CatalystPMS.Infrastructure.Data;
using CatalystPMS.Features.Products.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Features.Products.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int productId) =>
        await _context.Products.FindAsync(productId);

    public async Task<Product?> GetByIdWithDetailsAsync(int productId) =>
        await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Status)
            .Include(p => p.Specifications)
            .Include(p => p.ApprovalWorkflows.OrderByDescending(a => a.ActionDate))
            .Include(p => p.AuditLogs.OrderByDescending(a => a.LoggedAt))
            .FirstOrDefaultAsync(p => p.ProductId == productId);

    public async Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null)
    {
        var query = _context.Products.Where(p => p.Sku == sku);
        if (excludeProductId.HasValue)
            query = query.Where(p => p.ProductId != excludeProductId.Value);
        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Product>> GetByUserAsync(string userId) =>
        await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Status)
            .Where(p => p.CreatedByUserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

    public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? searchTerm, int? categoryId, int? statusId, int page, int pageSize)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Status)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(p =>
                p.Name.Contains(searchTerm) ||
                p.Sku.Contains(searchTerm) ||
                p.Brand.Contains(searchTerm));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (statusId.HasValue)
            query = query.Where(p => p.StatusId == statusId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Product product) =>
        await _context.Products.AddAsync(product);

    public void Update(Product product) =>
        _context.Products.Update(product);

    public async Task<int> CountByStatusAsync(int statusId) =>
        await _context.Products.CountAsync(p => p.StatusId == statusId);

    public async Task<IEnumerable<Product>> GetRecentAsync(int count) =>
        await _context.Products
            .Include(p => p.Status)
            .OrderByDescending(p => p.UpdatedAt)
            .Take(count)
            .ToListAsync();
}