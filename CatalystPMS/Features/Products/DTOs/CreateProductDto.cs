using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace CatalystPMS.Features.Products.DTOs
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int CategoryId { get; set; }
        public List<ProductSpecificationDto> Specifications { get; set; } = new();
        // Image arrives as IFormFile — handled separately in the controller
    }

  

public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int CategoryId { get; set; }
        public List<ProductSpecificationDto> Specifications { get; set; } = new();
        public IFormFile? Image { get; set; }
    }
}
