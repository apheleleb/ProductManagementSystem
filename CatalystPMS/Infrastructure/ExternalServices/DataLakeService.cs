// DataLakeService.cs
using System.Text;
using System.Text.Json;
using Azure.Storage;
using Azure.Storage.Files.DataLake;
using CatalystPMS.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CatalystPMS.Infrastructure.ExternalServices
{
    public class DataLakeService : IDataLakeService
    {
        private readonly DataLakeFileSystemClient _fileSystemClient;
        private readonly ILogger<DataLakeService> _logger;
        private const string DirectoryName = "published-products";

        public DataLakeService(IConfiguration configuration, ILogger<DataLakeService> logger)
        {
            _logger = logger;

            var accountName = configuration["DataLake:AccountName"]
                ?? throw new InvalidOperationException("DataLake:AccountName is missing from appsettings.json");
            var accountKey = configuration["DataLake:AccountKey"]
                ?? throw new InvalidOperationException("DataLake:AccountKey is missing from appsettings.json");
            var fileSystemName = configuration["DataLake:FileSystemName"] ?? "catalystpms";

            var serviceUri = new Uri($"https://{accountName}.dfs.core.windows.net");
            var credential = new StorageSharedKeyCredential(accountName, accountKey);
            var serviceClient = new DataLakeServiceClient(serviceUri, credential);

            _fileSystemClient = serviceClient.GetFileSystemClient(fileSystemName);
            _fileSystemClient.CreateIfNotExists();
        }

        public async Task SyncProductAsync(Product product)
        {
            try
            {
                var record = new
                {
                    product.ProductId,
                    product.Name,
                    product.Description,
                    product.Sku,
                    product.Brand,
                    product.UnitPrice,
                    CategoryName = product.Category?.Name ?? string.Empty,
                    Specifications = product.Specifications
                        .Select(s => new { s.Key, s.Value })
                        .ToList(),
                    PublishedAt = DateTime.UtcNow
                };

                var directoryClient = _fileSystemClient.GetDirectoryClient(DirectoryName);
                await directoryClient.CreateIfNotExistsAsync();

                var fileClient = directoryClient.GetFileClient($"{product.ProductId}.json");
                var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

                await fileClient.UploadAsync(stream, overwrite: true);
                _logger.LogInformation("Synced product {ProductId} to Data Lake.", product.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync product {ProductId} to Data Lake.", product.ProductId);
            }
        }

        public async Task RemoveProductAsync(int productId)
        {
            try
            {
                var directoryClient = _fileSystemClient.GetDirectoryClient(DirectoryName);
                var fileClient = directoryClient.GetFileClient($"{productId}.json");
                await fileClient.DeleteIfExistsAsync();
                _logger.LogInformation("Removed product {ProductId} from Data Lake.", productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove product {ProductId} from Data Lake.", productId);
            }
        }
    }
}