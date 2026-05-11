using Dapper;
using DocumentTracker.Models;
using Npgsql;

namespace DocumentTracker.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ProductRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<Product>> ListActiveAsync()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        var products = await connection.QueryAsync<Product>(ProductSql.ListActiveProducts);
        return products.AsList();
    }
}
