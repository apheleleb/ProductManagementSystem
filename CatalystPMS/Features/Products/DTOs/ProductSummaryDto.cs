using Microsoft.AspNetCore.Mvc;

namespace CatalystPMS.Features.Products.DTOs
{
    public class ProductSummaryDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
    }
}
