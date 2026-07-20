using CatalystPMS.Features.Categories.Repositories;
using CatalystPMS.Features.Products.Repositories;

namespace CatalystPMS.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    Task<int> SaveChangesAsync();
}