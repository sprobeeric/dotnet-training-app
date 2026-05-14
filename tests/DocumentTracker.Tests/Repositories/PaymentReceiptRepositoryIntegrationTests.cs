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
        await connection.ExecuteAsync("DELETE FROM payment_receipts;");
        await connection.ExecuteAsync(seedSql);

        var repository = new PaymentReceiptRepository(dataSource, new Mock<ILogger<PaymentReceiptRepository>>().Object);

        var result = await repository.SearchAsync(new PaymentReceiptSearchCriteria
        {
            Sort = "payment_date",
            Order = "desc",
            Page = 0,
            PageSize = 0
        });

        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, paymentReceipt => paymentReceipt.ReceiptNumber == "PR-1001");
        Assert.True(result.Total > 0);
    }

    [Fact]
    public async Task SearchAsync_WithSearchAndDateFilter_ReturnsMatchingPaymentReceipts()
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
        await connection.ExecuteAsync("DELETE FROM payment_receipts;");
        await connection.ExecuteAsync(seedSql);

        var repository = new PaymentReceiptRepository(dataSource, new Mock<ILogger<PaymentReceiptRepository>>().Object);

        var result = await repository.SearchAsync(new PaymentReceiptSearchCriteria
        {
            SearchTerm = "Northwind",
            DateFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(-8)),
            DateTo = DateOnly.FromDateTime(DateTime.Today.AddDays(-6)),
            Sort = "receipt_number",
            Order = "asc",
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(4, result.Total);
        Assert.Collection(
            result.Items,
            paymentReceipt => Assert.Equal("PR-1006", paymentReceipt.ReceiptNumber),
            paymentReceipt => Assert.Equal("PR-1008", paymentReceipt.ReceiptNumber),
            paymentReceipt => Assert.Equal("PR-1022", paymentReceipt.ReceiptNumber),
            paymentReceipt => Assert.Equal("PR-1024", paymentReceipt.ReceiptNumber));
    }

    [Fact]
    public async Task CreateAsync_WithPartialPayment_KeepsInvoicePending()
    {
        var dataSource = await CreateCleanDataSourceAsync();
        if (dataSource is null)
        {
            return;
        }

        await using (dataSource)
        await using (var connection = await dataSource.OpenConnectionAsync())
        {
            var invoiceId = await CreateInvoiceAsync(connection, "INV-PARTIAL", 1375m);
            var repository = new PaymentReceiptRepository(dataSource, new Mock<ILogger<PaymentReceiptRepository>>().Object);

            await repository.CreateAsync(CreateReceipt(invoiceId, "RCT-PARTIAL", 250m));

            var status = await connection.ExecuteScalarAsync<string>("SELECT status FROM invoices WHERE id = @InvoiceId;", new { InvoiceId = invoiceId });

            Assert.Equal("Pending", status);
        }
    }

    [Fact]
    public async Task CreateAsync_WithFullyPaidInvoice_MarksInvoicePaid()
    {
        var dataSource = await CreateCleanDataSourceAsync();
        if (dataSource is null)
        {
            return;
        }

        await using (dataSource)
        await using (var connection = await dataSource.OpenConnectionAsync())
        {
            var invoiceId = await CreateInvoiceAsync(connection, "INV-FULL", 1375m);
            var repository = new PaymentReceiptRepository(dataSource, new Mock<ILogger<PaymentReceiptRepository>>().Object);

            await repository.CreateAsync(CreateReceipt(invoiceId, "RCT-FULL", 1375m));

            var status = await connection.ExecuteScalarAsync<string>("SELECT status FROM invoices WHERE id = @InvoiceId;", new { InvoiceId = invoiceId });

            Assert.Equal("Paid", status);
        }
    }

    [Fact]
    public async Task SoftDeleteAsync_WithExistingReceipt_SetsDeletedAtUtc()
    {
        var dataSource = await CreateCleanDataSourceAsync();
        if (dataSource is null)
        {
            return;
        }

        await using (dataSource)
        await using (var connection = await dataSource.OpenConnectionAsync())
        {
            var invoiceId = await CreateInvoiceAsync(connection, "INV-DELETE", 1375m);
            var repository = new PaymentReceiptRepository(dataSource, new Mock<ILogger<PaymentReceiptRepository>>().Object);
            var receiptId = await repository.CreateAsync(CreateReceipt(invoiceId, "RCT-DELETE", 250m));
            var deletedAtUtc = new DateTime(2026, 5, 14, 8, 30, 0, DateTimeKind.Utc);

            var deleted = await repository.SoftDeleteAsync(receiptId, deletedAtUtc);
            var deletedAt = await connection.ExecuteScalarAsync<DateTime?>(
                "SELECT deleted_at_utc FROM payment_receipts WHERE id = @Id;",
                new { Id = receiptId });

            Assert.True(deleted);
            Assert.Equal(deletedAtUtc, deletedAt);
        }
    }

    [Fact]
    public async Task SoftDeleteAsync_WithFullyPaidInvoice_SetsInvoiceBackToPending()
    {
        var dataSource = await CreateCleanDataSourceAsync();
        if (dataSource is null)
        {
            return;
        }

        await using (dataSource)
        await using (var connection = await dataSource.OpenConnectionAsync())
        {
            var invoiceId = await CreateInvoiceAsync(connection, "INV-DELETE-PAID", 1375m);
            var repository = new PaymentReceiptRepository(dataSource, new Mock<ILogger<PaymentReceiptRepository>>().Object);
            var receiptId = await repository.CreateAsync(CreateReceipt(invoiceId, "RCT-DELETE-PAID", 1375m));

            var deleted = await repository.SoftDeleteAsync(receiptId, new DateTime(2026, 5, 14, 8, 30, 0, DateTimeKind.Utc));
            var status = await connection.ExecuteScalarAsync<string>(
                "SELECT status FROM invoices WHERE id = @InvoiceId;",
                new { InvoiceId = invoiceId });

            Assert.True(deleted);
            Assert.Equal("Pending", status);
        }
    }

    private static async Task<NpgsqlDataSource?> CreateCleanDataSourceAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("DOCUMENTTRACKER_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var connection = await dataSource.OpenConnectionAsync();

        var root = FindRepositoryRoot();
        var schemaSql = await File.ReadAllTextAsync(Path.Combine(root, "database", "schema.sql"));

        await connection.ExecuteAsync(schemaSql);
        await connection.ExecuteAsync("DELETE FROM payment_receipts;");
        await connection.ExecuteAsync("DELETE FROM invoices;");

        return dataSource;
    }

    private static async Task<int> CreateInvoiceAsync(NpgsqlConnection connection, string invoiceNumber, decimal totalAmount) =>
        await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO invoices
                (invoice_number, customer_name, invoice_date, due_date, status, subtotal, tax_amount, total_amount, created_at_utc, updated_at_utc)
            VALUES
                (@InvoiceNumber, 'Repository Test Customer', current_date, current_date + 30, 'Pending', @TotalAmount, 0, @TotalAmount, now() at time zone 'utc', now() at time zone 'utc')
            RETURNING id;
            """,
            new { InvoiceNumber = invoiceNumber, TotalAmount = totalAmount });

    private static PaymentReceipt CreateReceipt(int invoiceId, string receiptNumber, decimal amountPaid) => new()
    {
        ReceiptNumber = receiptNumber,
        InvoiceId = invoiceId,
        PaymentDate = new DateOnly(2026, 5, 13),
        AmountPaid = amountPaid,
        PaymentMethod = "Cash",
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

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
