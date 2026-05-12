using Dapper;
using DocumentTracker.Models;
using DocumentTracker.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace DocumentTracker.Tests.Repositories;

public class PaymentReceiptRepositoryIntegrationTests
{
    [Fact]
    public async Task SearchAsync_WithInvalidPaging_StillReturnsSeededRows()
    {
        var connectionString = Environment.GetEnvironmentVariable("DOCUMENTTRACKER_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        var root = FindRepositoryRoot();
        var schemaSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "schema.sql"));
        var seedSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "seed.sql"));

        await connection.ExecuteAsync(schemaSql);
        await connection.ExecuteAsync("DELETE FROM payment_receipt_products;");
        await connection.ExecuteAsync("DELETE FROM payment_receipts;");
        await connection.ExecuteAsync("DELETE FROM products;");
        await connection.ExecuteAsync(seedSql);

        var repository = new PaymentReceiptRepository(dataSource, new Mock<ILogger<PaymentReceiptRepository>>().Object);

        var result = await repository.SearchAsync(null, null, null, "payment_date_utc", "desc", 0, 0);

        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, paymentReceipt => paymentReceipt.ReceiptNumber == "PR-20260512-000001");
        Assert.True(result.Total > 0);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DocumentTracker.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Could not locate the repository root.");
        }

        return directory.FullName;
    }
}
