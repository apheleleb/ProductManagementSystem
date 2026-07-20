namespace CatalystPMS.Core.Models
{
    public class ProductSpecification
    {
        public int SpecificationId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
