using CatalystPMS.Core.Models;
using CatalystPMS.Features.Categories.DTOs;
using CatalystPMS.Infrastructure.Data;
using CatalystPMS.Core.Interfaces;

namespace CatalystPMS.Features.Categories.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _uow;

        public CategoryService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
        {
            var categories = await _uow.Categories.GetAllActiveAsync();
            return categories.Select(c => new CategoryResponseDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive
            });
        }

        public async Task<(bool Success, string? Error, int CategoryId)> CreateAsync(
            CreateCategoryDto dto, string userId)
        {
            if (await _uow.Categories.NameExistsAsync(dto.Name))
                return (false, $"Category '{dto.Name}' already exists.", 0);

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedByUserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Categories.AddAsync(category);
            await _uow.SaveChangesAsync();
            return (true, null, category.CategoryId);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(int categoryId, UpdateCategoryDto dto)
        {
            var category = await _uow.Categories.GetByIdAsync(categoryId);
            if (category == null) return (false, "Category not found.");

            if (await _uow.Categories.NameExistsAsync(dto.Name, categoryId))
                return (false, $"Category name '{dto.Name}' is already in use.");

            category.Name = dto.Name;
            category.Description = dto.Description;
            _uow.Categories.Update(category);
            await _uow.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeactivateAsync(int categoryId)
        {
            var category = await _uow.Categories.GetByIdAsync(categoryId);
            if (category == null) return (false, "Category not found.");

            if (await _uow.Categories.HasProductsAsync(categoryId))
                return (false, "Cannot deactivate a category that has products assigned to it.");

            category.IsActive = false;
            _uow.Categories.Update(category);
            await _uow.SaveChangesAsync();
            return (true, null);
        }
    }
}
