using CatalystPMS.Core.Models;
using CatalystPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Features.Categories.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllActiveAsync() =>
            await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<Category?> GetByIdAsync(int categoryId) =>
            await _context.Categories.FindAsync(categoryId);

        public async Task<bool> NameExistsAsync(string name, int? excludeCategoryId = null)
        {
            var query = _context.Categories.Where(c => c.Name == name);
            if (excludeCategoryId.HasValue)
                query = query.Where(c => c.CategoryId != excludeCategoryId.Value);
            return await query.AnyAsync();
        }

        public async Task<bool> HasProductsAsync(int categoryId) =>
            await _context.Products.AnyAsync(p => p.CategoryId == categoryId);

        public async Task AddAsync(Category category) =>
            await _context.Categories.AddAsync(category);

        public void Update(Category category) =>
            _context.Categories.Update(category);
    }
}
