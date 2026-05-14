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
        var normalizedDateFrom = criteria.DateFrom?.ToDateTime(TimeOnly.MinValue);
        var normalizedDateTo = criteria.DateTo?.ToDateTime(TimeOnly.MinValue);
        var offset = (normalizedPage - 1) * normalizedPageSize;

        var allowedSorts = new Dictionary<string, string>
        {
            ["receipt_number"] = "pr.receipt_number",
            ["invoice_number"] = "i.invoice_number",
            ["customer_name"] = "i.customer_name",
            ["payment_date"] = "pr.payment_date",
            ["amount_paid"] = "pr.amount_paid",
            ["payment_method"] = "pr.payment_method",
            ["reference_number"] = "pr.reference_number",
            ["notes"] = "pr.notes"
        };

        var sortBy = allowedSorts.GetValueOrDefault(criteria.Sort ?? string.Empty, "pr.payment_date");
        var orderBy = string.Equals(criteria.Order, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        var parameters = new DynamicParameters();

        parameters.Add("SearchTerm", normalizedSearch);
        parameters.Add("SearchPattern", normalizedSearch is null ? null : $"%{normalizedSearch}%");
        parameters.Add("DateFrom", normalizedDateFrom);
        parameters.Add("DateTo", normalizedDateTo);
        parameters.Add("PageSize", normalizedPageSize);
        parameters.Add("Offset", offset);

        await using var connection = await _dataSource.OpenConnectionAsync();

        var sql = PaymentReceiptSql.SearchPaymentReceipts(sortBy, orderBy);
        var receipts = await connection.QueryAsync<PaymentReceipt>(
            sql,
            parameters);

        var total = await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.CountPaymentReceipts,
            parameters);

        return new PaginatedResult<PaymentReceipt>
        {
            Items = receipts.AsList(),
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

    public async Task<bool> ReferenceNumberExistsAsync(string referenceNumber, int? excludeId = null)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            PaymentReceiptSql.ReferenceNumberExists,
            new { ReferenceNumber = referenceNumber, ExcludeId = excludeId });
    }

    public async Task<bool> InvoiceExistsAndPendingAsync(int invoiceId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            PaymentReceiptSql.InvoiceExistsAndPending,
            new { InvoiceId = invoiceId });
    }

    public async Task<int> CreateAsync(PaymentReceipt paymentReceipt)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

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
            },
            transaction);

        await connection.ExecuteAsync(
            PaymentReceiptSql.UpdateInvoiceStatusToPaid,
            new
            {
                PaymentReceiptId = id,
                paymentReceipt.InvoiceId,
                paymentReceipt.AmountPaid,
                paymentReceipt.UpdatedAtUtc
            },
            transaction);

        await transaction.CommitAsync();

        _logger.LogInformation("Created payment receipt with id {PaymentReceiptId}.", id);
        return id;
    }

   public async Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var invoiceId = await connection.ExecuteScalarAsync<int?>(
                PaymentReceiptSql.SoftDeletePaymentReceipt,
                new
                {
                    Id = id,
                    DeletedAtUtc = deletedAtUtc
                },
                transaction);

            if (invoiceId is null)
            {
                await transaction.RollbackAsync();

                _logger.LogInformation("Soft delete skipped for payment receipt with id {PaymentReceiptId}. No rows affected.", id);
                return false;
            }

            await connection.ExecuteAsync(
                PaymentReceiptSql.RecalculateInvoiceStatusAfterReceiptDelete,
                new
                {
                    InvoiceId = invoiceId.Value,
                    UpdatedAtUtc = deletedAtUtc
                },
                transaction);

            await transaction.CommitAsync();

            _logger.LogInformation("Soft deleted payment receipt with id {PaymentReceiptId}.", id);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
