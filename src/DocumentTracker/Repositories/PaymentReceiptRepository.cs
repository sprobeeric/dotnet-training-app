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

    public async Task<PaginatedResult<PaymentReceipt>> SearchAsync(
        string? searchTerm, 
        DateTime? dateFrom, 
        DateTime? dateTo,
        string sort,
        string order,
        int page, 
        int pageSize)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        var offset = (page - 1) * pageSize;

        var allowedSorts = new Dictionary<string, string>
        {
            ["receipt_number"] = "receipt_number",
            ["reference_number"] = "reference_number",
            ["payment_date_utc"] = "payment_date_utc",
            ["total_amount"] = "total_amount",
            ["received"] = "received",
            ["change_amount"] = "change_amount"
        };

        var sortBy = allowedSorts.GetValueOrDefault(
            sort ?? "",
            "payment_date_utc"
        );

        var orderBy = order?.ToLower() == "asc"
            ? "ASC"
            : "DESC";

        await using var connection = await _dataSource.OpenConnectionAsync();
        var sql = PaymentReceiptSql.SearchPaymentReceipts(sortBy, orderBy);
        var paymentReceipts = await connection.QueryAsync<PaymentReceipt>(
            sql,
            new
            {
                SearchTerm = normalizedSearch,
                SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%",
                DateFrom = dateFrom,
                DateTo = dateTo?.Date.AddDays(1),
                PageSize = pageSize,
                Offset = offset
            });

        var total = await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.CountPaymentReceipts,
            new
            {
                SearchTerm = normalizedSearch,
                SearchPattern = normalizedSearch is null
                    ? null
                    : $"%{normalizedSearch}%",

                DateFrom = dateFrom?.Date,
                DateTo = dateTo?.Date.AddDays(1)
            });

         return new PaginatedResult<PaymentReceipt>
        {
            Items = paymentReceipts.AsList(),
            Total = total
        };
    }
}
