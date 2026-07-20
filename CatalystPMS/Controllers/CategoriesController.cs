using CatalystPMS.Core.Enums;
using CatalystPMS.Features.Categories.DTOs;
using CatalystPMS.Features.Categories.Services;
using CatalystPMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CatalystPMS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private string CurrentUserId => User.FindFirstValue("sub")!;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>Get all active categories. Both roles can view.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<CategoryResponseDto>>.Ok(categories));
        }

        /// <summary>Create a new category. Manager only.</summary>
        [HttpPost]
        [Authorize(Roles = UserRoles.ProductManager)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            var (success, error, categoryId) = await _categoryService.CreateAsync(dto, CurrentUserId);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return CreatedAtAction(nameof(GetAll), new { id = categoryId },
                ApiResponse<object>.Ok(new { categoryId }));
        }

        /// <summary>Update category name or description. Manager only.</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = UserRoles.ProductManager)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var (success, error) = await _categoryService.UpdateAsync(id, dto);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Category updated."));
        }

        /// <summary>Deactivate a category (soft delete). Only if no products assigned.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = UserRoles.ProductManager)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var (success, error) = await _categoryService.DeactivateAsync(id);
            if (!success) return BadRequest(ApiResponse.Fail(error!));
            return Ok(ApiResponse.OkNoData("Category deactivated."));
        }
    }
}