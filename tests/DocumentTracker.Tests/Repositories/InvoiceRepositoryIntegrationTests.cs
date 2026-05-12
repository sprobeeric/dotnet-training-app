using Dapper;
using DocumentTracker.Models;
using DocumentTracker.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace DocumentTracker.Tests.Repositories;

public class InvoiceRepositoryIntegrationTests
{
    [Fact]
    public async Task SearchAsync_WithPostgreSqlConnection_CanFilterByInvoiceDateRange()
    {
        DapperDateOnlyTypeHandler.Register();

        var connectionString = Environment.GetEnvironmentVariable("DOCUMENTTRACKER_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        var root = FindRepositoryRoot();
        var schemaSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "schema.sql"));

        await connection.ExecuteAsync(schemaSql);
        await connection.ExecuteAsync("DELETE FROM invoices;");

        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(
            """
            INSERT INTO invoices
                (invoice_number, customer_name, invoice_date, due_date, status, subtotal, tax_amount, total_amount, notes, created_at_utc, updated_at_utc)
            VALUES
                (@InvoiceNumber, @CustomerName, @InvoiceDate, @DueDate, @Status, @Subtotal, @TaxAmount, @TotalAmount, @Notes, @CreatedAtUtc, @UpdatedAtUtc);
            """,
            new
            {
                InvoiceNumber = $"INV-{Guid.NewGuid():N}"[..12],
                CustomerName = "Integration Customer",
                InvoiceDate = new DateOnly(2026, 5, 1),
                DueDate = new DateOnly(2026, 5, 15),
                Status = InvoiceStatus.Sent.ToString(),
                Subtotal = 100m,
                TaxAmount = 12m,
                TotalAmount = 112m,
                Notes = "Created by the invoice repository integration test.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        await connection.ExecuteAsync(
            """
            INSERT INTO invoices
                (invoice_number, customer_name, invoice_date, due_date, status, subtotal, tax_amount, total_amount, notes, created_at_utc, updated_at_utc)
            VALUES
                (@InvoiceNumber, @CustomerName, @InvoiceDate, @DueDate, @Status, @Subtotal, @TaxAmount, @TotalAmount, @Notes, @CreatedAtUtc, @UpdatedAtUtc);
            """,
            new
            {
                InvoiceNumber = $"INV-{Guid.NewGuid():N}"[..12],
                CustomerName = "Integration Customer",
                InvoiceDate = new DateOnly(2026, 6, 1),
                DueDate = new DateOnly(2026, 6, 15),
                Status = InvoiceStatus.Sent.ToString(),
                Subtotal = 200m,
                TaxAmount = 24m,
                TotalAmount = 224m,
                Notes = "Outside the requested invoice date range.",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        var repository = new InvoiceRepository(dataSource, new Mock<ILogger<InvoiceRepository>>().Object);

        var invoices = await repository.SearchAsync(
            "Integration Customer",
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31));

        Assert.Contains(invoices, invoice => invoice.CustomerName == "Integration Customer" && invoice.TotalAmount == 112m);
        Assert.DoesNotContain(invoices, invoice => invoice.CustomerName == "Integration Customer" && invoice.TotalAmount == 224m);
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