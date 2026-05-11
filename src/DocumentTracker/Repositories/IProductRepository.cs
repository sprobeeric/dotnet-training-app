using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> ListActiveAsync();
}
