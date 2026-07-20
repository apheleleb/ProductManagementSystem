using CatalystPMS.Core.Models;
using CatalystPMS.Features.Approvals.DTOs;
using CatalystPMS.Features.AuditLogs.DTOs;
using CatalystPMS.Features.Products.DTOs;
using CatalystPMS.Features.Products.Repositories;
using CatalystPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace CatalystPMS.Features.Products.DTOs;

public class ProductResponseDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool HasImage { get; set; }
    public List<ProductSpecificationDto> Specifications { get; set; } = new();
    public List<ApprovalHistoryDto> ApprovalHistory { get; set; } = new();
    public List<AuditEntryDto> AuditLog { get; set; } = new();
}