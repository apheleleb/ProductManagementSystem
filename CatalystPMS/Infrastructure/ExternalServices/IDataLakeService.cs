using CatalystPMS.Core.Models;
using CatalystPMS.Shared.DTOs;
using CatalystPMS.Infrastructure.Data;

namespace CatalystPMS.Infrastructure.ExternalServices
{
    /// <summary>
    /// Syncs approved/published product data to Azure Data Lake Storage Gen2
    /// for fast, decoupled retrieval by external systems (Client Portal, OMS)
    /// without hitting the transactional database directly.
    /// </summary>
    public interface IDataLakeService
    {
        /// <summary>
        /// Writes (or overwrites) a JSON snapshot of a product to the data lake.
        /// Call this when a product is Published.
        /// </summary>
        Task SyncProductAsync(Product product);

        /// <summary>
        /// Removes a product's snapshot from the data lake — call when a
        /// product is Unpublished or Archived, so the lake only ever reflects
        /// what's genuinely live.
        /// </summary>
        Task RemoveProductAsync(int productId);
    }

    /// <summary>
    /// Flat, denormalized shape written to the data lake — deliberately
    /// simpler than your full Product entity, since consumers of the lake
    /// (other systems) want fast reads of published-catalog data, not your
    /// internal workflow/audit structure.
    /// </summary>
    public class ProductDataLakeRecord
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<KeyValuePair<string, string>> Specifications { get; set; } = new();
        public DateTime PublishedAt { get; set; }
    }
}
