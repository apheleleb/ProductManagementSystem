using CatalystPMS.Core.Models;
using CatalystPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CatalystPMS.Features.Categories.Repositories;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllActiveAsync();
    Task<Category?> GetByIdAsync(int categoryId);
    Task<bool> NameExistsAsync(string name, int? excludeCategoryId = null);
    Task<bool> HasProductsAsync(int categoryId);
    Task AddAsync(Category category);
    void Update(Category category);
}

