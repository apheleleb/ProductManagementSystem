using CatalystPMS.Core.Enums;
using CatalystPMS.Features.Approvals.DTOs;
using CatalystPMS.Features.AuditLogs.DTOs;
using CatalystPMS.Features.Products.DTOs;
using CatalystPMS.Features.Products.Services;
using CatalystPMS.Infrastructure.Data;
using CatalystPMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CatalystPMS.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly AppDbContext _context;

    public ProductsController(IProductService productService, AppDbContext context)
    {
        _productService = productService;
        _context = context;
    }

    private string CurrentUserId => User.FindFirstValue("sub")
        ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Search and filter the product catalog.</summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? statusId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _productService.SearchAsync(search, categoryId, statusId, page, pageSize);
        return Ok(ApiResponse<PagedResponseDto<ProductSummaryDto>>.Ok(result));
    }

    /// <summary>Get full product details including specs, approval history and audit log.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound(ApiResponse.Fail("Product not found."));
        return Ok(ApiResponse<ProductResponseDto>.Ok(product));
    }

    /// <summary>Get the current capturer's draft products.</summary>
    [HttpGet("my-drafts")]
    [Authorize(Roles = UserRoles.ProductCapturer)]
    public async Task<IActionResult> GetMyDrafts()
    {
        var drafts = await _productService.GetMyDraftsAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<ProductSummaryDto>>.Ok(drafts));
    }

    /// <summary>
    /// Create a new product. Use multipart/form-data.
    /// Pass submitForApproval=true to submit immediately, false to save as draft.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.ProductCapturer)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] CreateProductRequest request,
        [FromQuery] bool submitForApproval = false)
    {
        var dto = new CreateProductDto
        {
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Brand = request.Brand,
            UnitPrice = request.UnitPrice,
            CategoryId = request.CategoryId,
            Specifications = request.Specifications
        };

        var (success, error, productId) = await _productService.CreateAsync(
            dto, CurrentUserId, request.Image, submitForApproval);

        if (!success) return BadRequest(ApiResponse.Fail(error!));

        return CreatedAtAction(nameof(GetById), new { id = productId },
            ApiResponse<object>.Ok(new { productId }));
    }

    /// <summary>Update an existing product (Draft, Pending Approval, or Rejected only).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRoles.ProductCapturer)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] UpdateProductRequest request)
    {
        var dto = new UpdateProductDto
        {
            Name = request.Name,
            Description = request.Description,
            Brand = request.Brand,
            UnitPrice = request.UnitPrice,
            CategoryId = request.CategoryId,
            Specifications = request.Specifications
        };

        var (success, error) = await _productService.UpdateAsync(id, dto, CurrentUserId, request.Image);
        if (!success) return BadRequest(ApiResponse.Fail(error!));
        return Ok(ApiResponse.OkNoData("Product updated successfully."));
    }

    /// <summary>Submit a draft product for manager approval.</summary>
    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = UserRoles.ProductCapturer)]
    public async Task<IActionResult> Submit(int id)
    {
        var (success, error) = await _productService.SubmitForApprovalAsync(id, CurrentUserId);
        if (!success) return BadRequest(ApiResponse.Fail(error!));
        return Ok(ApiResponse.OkNoData("Product submitted for approval."));
    }

    /// <summary>Serve the product image as a binary response.</summary>
    [HttpGet("{id:int}/image")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImage(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null || product.ImageData == null)
            return NotFound();

        return File(product.ImageData, product.ImageMimeType ?? "image/jpeg");
    }

    /// <summary>Dashboard stats — product counts by status.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            Total = await _context.Products.CountAsync(),
            PendingApproval = await _productService.CountByStatusAsync((int)ProductStatusEnum.PendingApproval),
            Approved = await _productService.CountByStatusAsync((int)ProductStatusEnum.Approved),
            Rejected = await _productService.CountByStatusAsync((int)ProductStatusEnum.Rejected),
            Active = await _productService.CountByStatusAsync((int)ProductStatusEnum.Active)
        };
        return Ok(ApiResponse<object>.Ok(stats));
    }

    /// <summary>Recent activity feed for dashboard.</summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 5)
    {
        var recent = await _productService.GetRecentActivityAsync(count);
        return Ok(ApiResponse<IEnumerable<ProductSummaryDto>>.Ok(recent));
    }

    /// <summary>
    /// Reads a published product directly from the Data Lake rather than the
    /// database — demonstrates the "quick retrieval" requirement.
    /// </summary>
    [HttpGet("{id:int}/from-data-lake")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFromDataLake(int id, [FromServices] IConfiguration config)
    {
        var accountName = config["DataLake:AccountName"];
        var accountKey = config["DataLake:AccountKey"];
        var fileSystemName = config["DataLake:FileSystemName"] ?? "catalystpms";

        var serviceUri = new Uri($"https://{accountName}.dfs.core.windows.net");
        var credential = new Azure.Storage.StorageSharedKeyCredential(accountName, accountKey);
        var serviceClient = new Azure.Storage.Files.DataLake.DataLakeServiceClient(serviceUri, credential);
        var fileClient = serviceClient
            .GetFileSystemClient(fileSystemName)
            .GetDirectoryClient("published-products")
            .GetFileClient($"{id}.json");

        if (!await fileClient.ExistsAsync())
            return NotFound(ApiResponse.Fail("Product not found in Data Lake (not published, or not yet synced)."));

        var download = await fileClient.ReadAsync();
        using var reader = new StreamReader(download.Value.Content);
        var json = await reader.ReadToEndAsync();

        return Content(json, "application/json");
    }

    /// <summary>List soft-deleted products (the "recycle bin"). Manager only.</summary>
    [HttpGet("deleted")]
    [Authorize(Roles = UserRoles.ProductManager)]
    public async Task<IActionResult> GetDeleted([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted)
            .Include(p => p.Category)
            .OrderByDescending(p => p.DeletedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ArchivedProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Sku = p.Sku,
                Brand = p.Brand,
                CategoryName = p.Category.Name,
                UnitPrice = p.UnitPrice,
                DeletedAt = p.DeletedAt!.Value,
                DeletedByUserId = p.DeletedByUserId ?? string.Empty,
                CreatedByUserId = p.CreatedByUserId
            })
            .ToListAsync();

        var result = new PagedResponseDto<ArchivedProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResponseDto<ArchivedProductDto>>.Ok(result));
    }

    /// <summary>Get full details of a single soft-deleted product. Manager only.</summary>
    [HttpGet("deleted/{id:int}")]
    [Authorize(Roles = UserRoles.ProductManager)]
    public async Task<IActionResult> GetDeletedById(int id)
    {
        // Reuses your existing IProductService.GetByIdAsync mapping logic where possible —
        // if that method queries via a repository that also respects the global filter,
        // you'll need a filter-bypassing variant there too. Simplest fix shown here
        // queries directly against the context.
        var product = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.ProductId == id && p.IsDeleted)
            .Include(p => p.Category)
            .Include(p => p.Status)
            .Include(p => p.Specifications)
            .Include(p => p.ApprovalWorkflows)
            .Include(p => p.AuditLogs)
            .FirstOrDefaultAsync();

        if (product == null) return NotFound(ApiResponse.Fail("Deleted product not found."));

        // Map to ProductResponseDto the same way your existing GetById action does —
        // plug in your actual mapping call/AutoMapper profile here instead of this
        // inline example if you have one already.
        var dto = new ProductResponseDto
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            Brand = product.Brand,
            UnitPrice = product.UnitPrice,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            StatusId = product.StatusId,
            StatusName = product.Status.StatusName,
            CreatedByUserId = product.CreatedByUserId,
            ApprovedByUserId = product.ApprovedByUserId,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            HasImage = product.ImageData != null,
            Specifications = product.Specifications
                .Select(s => new ProductSpecificationDto { Key = s.Key, Value = s.Value })
                .ToList(),
            ApprovalHistory = product.ApprovalWorkflows
                .OrderBy(a => a.ActionDate)
                .Select(a => new ApprovalHistoryDto
                {
                    Action = a.Action,
                    Comment = a.Comment,
                    ActorUserId = a.ActorUserId,
                    ActionDate = a.ActionDate
                }).ToList(),
            AuditLog = product.AuditLogs
                .OrderBy(a => a.LoggedAt)
                .Select(a => new AuditEntryDto
                {
                    ActionType = a.ActionType,
                    FieldName = a.FieldName,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    ActorUserId = a.ActorUserId,
                    LoggedAt = a.LoggedAt
                }).ToList()
        };

        return Ok(ApiResponse<ProductResponseDto>.Ok(dto));
    }
}
