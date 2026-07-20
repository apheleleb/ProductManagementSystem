using CatalystPMS.Features.Categories.DTOs;

namespace CatalystPMS.Features.Categories.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
        Task<(bool Success, string? Error, int CategoryId)> CreateAsync(CreateCategoryDto dto, string userId);
        Task<(bool Success, string? Error)> UpdateAsync(int categoryId, UpdateCategoryDto dto);
        Task<(bool Success, string? Error)> DeactivateAsync(int categoryId);
    }
}
