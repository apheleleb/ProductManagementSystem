namespace CatalystPMS.Core.Models
{
    public class ProductStatus
    {
            public int StatusId { get; set; }
            public string StatusName { get; set; } = string.Empty;

            public ICollection<Product> Products { get; set; } = new List<Product>();
        
    }
}
