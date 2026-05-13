using Dapper;
using DocumentTracker.Models;
using Npgsql;

namespace DocumentTracker.Repositories;

public class PaymentReceiptRepository : IPaymentReceiptRepository, IPaymentReceiptNumberSequenceProvider
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PaymentReceiptRepository> _logger;

    public PaymentReceiptRepository(NpgsqlDataSource dataSource, ILogger<PaymentReceiptRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<PaginatedResult<PaymentReceipt>> SearchAsync(string? searchTerm, DateOnly? dateFrom, DateOnly? dateTo, string sort, string order, int page, int pageSize)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;
        var offset = (normalizedPage - 1) * normalizedPageSize;
        var dateFromValue = dateFrom?.ToDateTime(TimeOnly.MinValue);
        var dateToValue = dateTo?.ToDateTime(TimeOnly.MinValue);

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

        var sortBy = allowedSorts.GetValueOrDefault(sort ?? string.Empty, "payment_date");
        var orderBy = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        await using var connection = await _dataSource.OpenConnectionAsync();
        var sql = PaymentReceiptSql.SearchPaymentReceipts(sortBy, orderBy);
        var paymentReceipts = await connection.QueryAsync<PaymentReceipt>(
            sql,
            new
            {
                SearchTerm = normalizedSearch,
                SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%",
                DateFrom = dateFromValue,
                DateTo = dateToValue,
                PageSize = normalizedPageSize,
                Offset = offset
            });

        var total = await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.CountPaymentReceipts,
            new
            {
                SearchTerm = normalizedSearch,
                SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%",
                DateFrom = dateFromValue,
                DateTo = dateToValue
            });

        return new PaginatedResult<PaymentReceipt>
        {
            Items = paymentReceipts.AsList(),
            Total = total
        };
    }

    public async Task<PaymentReceipt?> GetByIdAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();

        return await connection.QuerySingleOrDefaultAsync<PaymentReceipt>(
            PaymentReceiptSql.GetById,
            new { Id = id });
    }

    public async Task<bool> ReceiptNumberExistsAsync(string receiptNumber, int? excludeId = null)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            PaymentReceiptSql.ReceiptNumberExists,
            new { ReceiptNumber = receiptNumber, ExcludeId = excludeId });
    }

    public async Task<bool> InvoiceExistsAndActiveAsync(int invoiceId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            PaymentReceiptSql.InvoiceExistsAndActive,
            new { InvoiceId = invoiceId });
    }

    public async Task<int> GetNextReceiptSequenceAsync()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COALESCE(MAX(CAST(SUBSTRING(receipt_number FROM '[0-9]+$') AS integer)), 1000) + 1
            FROM payment_receipts
            WHERE receipt_number ~ '^PR-[0-9]+$';
            """);
    }

    public async Task<int> CreateAsync(PaymentReceipt paymentReceipt)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();

        var id = await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.InsertPaymentReceipt,
            new
            {
                paymentReceipt.ReceiptNumber,
                paymentReceipt.InvoiceId,
                PaymentDate = paymentReceipt.PaymentDate.ToDateTime(TimeOnly.MinValue),
                paymentReceipt.AmountPaid,
                paymentReceipt.PaymentMethod,
                paymentReceipt.ReferenceNumber,
                paymentReceipt.Notes,
                paymentReceipt.CreatedAtUtc,
                paymentReceipt.UpdatedAtUtc
            });

        _logger.LogInformation("Created payment receipt with id {PaymentReceiptId}.", id);
        return id;
    }
}
