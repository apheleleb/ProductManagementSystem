namespace CatalystPMS.Features.Products.DTOs
{
    public class ArchivedProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public DateTime DeletedAt { get; set; }
        public string DeletedByUserId { get; set; } = string.Empty;
        public string CreatedByUserId { get; set; } = string.Empty;
    }
}
