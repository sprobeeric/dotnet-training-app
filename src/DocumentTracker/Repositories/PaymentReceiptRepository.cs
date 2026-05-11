using Dapper;
using DocumentTracker.Models;
using Npgsql;

namespace DocumentTracker.Repositories;

public class PaymentReceiptRepository : IPaymentReceiptRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PaymentReceiptRepository> _logger;

    public PaymentReceiptRepository(NpgsqlDataSource dataSource, ILogger<PaymentReceiptRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<int> GetNextReceiptSequenceAsync(DateOnly paymentDate)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.GetNextReceiptSequence,
            new { ReceiptPrefix = $"RCP-{paymentDate:yyyyMMdd}" });
    }

    public async Task<int> CreateAsync(PaymentReceipt paymentReceipt, IReadOnlyList<PaymentReceiptProduct> products)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var id = await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.InsertPaymentReceipt,
            new
            {
                paymentReceipt.ReceiptNumber,
                paymentReceipt.PaymentDateUtc,
                paymentReceipt.ReferenceNumber,
                paymentReceipt.TotalAmount,
                paymentReceipt.Received,
                paymentReceipt.ChangeAmount,
                paymentReceipt.CreatedAtUtc,
                paymentReceipt.UpdatedAtUtc
            },
            transaction);

        foreach (var product in products)
        {
            await connection.ExecuteAsync(
                PaymentReceiptSql.InsertPaymentReceiptProduct,
                new
                {
                    PaymentReceiptId = id,
                    product.ProductId,
                    product.Quantity,
                    product.UnitPrice,
                    product.LineTotal
                },
                transaction);
        }

        await transaction.CommitAsync();
        _logger.LogInformation("Created payment receipt with id {PaymentReceiptId}.", id);
        return id;
    }

    public async Task<PaymentReceipt?> GetByIdAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<PaymentReceipt>(PaymentReceiptSql.GetById, new { Id = id });
    }

    public async Task<IReadOnlyList<PaymentReceiptProduct>> ListProductsByReceiptIdAsync(int paymentReceiptId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        var products = await connection.QueryAsync<PaymentReceiptProduct, Product, PaymentReceiptProduct>(
            PaymentReceiptSql.ListProductsByReceiptId,
            (paymentReceiptProduct, product) =>
            {
                paymentReceiptProduct.Product = product;
                return paymentReceiptProduct;
            },
            new { PaymentReceiptId = paymentReceiptId },
            splitOn: "Id");

        return products.AsList();
    }
}
