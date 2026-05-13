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

    public async Task<PaginatedResult<PaymentReceipt>> SearchAsync(PaymentReceiptSearchCriteria criteria)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(criteria.SearchTerm) ? null : criteria.SearchTerm.Trim();

        var normalizedPage = criteria.Page < 1 ? 1 : criteria.Page;
        var normalizedPageSize = criteria.PageSize < 1 ? 10 : criteria.PageSize;
        var offset = (normalizedPage - 1) * normalizedPageSize;

        var allowedSorts = new Dictionary<string, string>
        {
            ["receipt_number"] = "pr.receipt_number",
            ["invoice_number"] = "i.invoice_number",
            ["payment_date"] = "pr.payment_date",
            ["amount_paid"] = "pr.amount_paid",
            ["payment_method"] = "pr.payment_method",
            ["reference_number"] = "pr.reference_number",
            ["notes"] = "pr.notes"
        };

        var sortBy = allowedSorts.GetValueOrDefault(criteria.Sort ?? string.Empty, "pr.payment_date");
        var orderBy = string.Equals(criteria.Order, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        await using var connection = await _dataSource.OpenConnectionAsync();
        var sql = PaymentReceiptSql.SearchPaymentReceipts(sortBy, orderBy);
        var parameters = new
        {
            SearchTerm = normalizedSearch,
            SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%",
            criteria.DateFrom,
            criteria.DateTo,
            PageSize = normalizedPageSize,
            Offset = offset
        };

        var receipts = await connection.QueryAsync<PaymentReceipt>(sql, parameters);

        var total = await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.CountPaymentReceipts,
            parameters
        );

        return new PaginatedResult<PaymentReceipt>
        {
            Items = receipts.AsList(),
            Total = total
        };
    }

    public async Task<PaymentReceipt?> GetByIdAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();

        var receipt = await connection.QuerySingleOrDefaultAsync<PaymentReceipt>(
            PaymentReceiptSql.GetById,
            new { Id = id });

        if (receipt is null)
        {
            return null;
        }

        var products = await connection.QueryAsync<PaymentReceiptProduct, Product, PaymentReceiptProduct>(
            PaymentReceiptSql.GetProductsByReceiptId,
            (receiptProduct, product) =>
            {
                receiptProduct.Product = product;
                return receiptProduct;
            },
            new { PaymentReceiptId = id },
            splitOn: "Id");

        receipt.PaymentReceiptProducts = products.AsList();
        return receipt;
    }

    public async Task<int> GetNextReceiptSequenceAsync(DateOnly paymentDate)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.GetNextReceiptSequence,
            new { ReceiptPrefix = $"PR-{paymentDate:yyyyMMdd}" });
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

    public async Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var rows = await connection.ExecuteAsync(
            PaymentReceiptSql.SoftDeletePaymentReceipt,
            new { Id = id, DeletedAtUtc = deletedAtUtc },
            transaction);

        await transaction.CommitAsync();
        _logger.LogInformation("Soft deleted payment receipt with id {PaymentReceiptId}. Rows affected: {RowsAffected}.", id, rows);
        return rows == 1;
    }
}
