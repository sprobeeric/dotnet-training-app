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

    public async Task<PaginatedResult<PaymentReceipt>> SearchAsync(string? searchTerm, DateOnly? dateFrom, DateOnly? dateTo, string sort, string order, int page, int pageSize)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;
        var normalizedDateFrom = dateFrom?.ToDateTime(TimeOnly.MinValue);
        var normalizedDateTo = dateTo?.ToDateTime(TimeOnly.MinValue);
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

        var sortBy = allowedSorts.GetValueOrDefault(sort ?? string.Empty, "payment_date");
        var orderBy = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        await using var connection = await _dataSource.OpenConnectionAsync();
        var parameters = new DynamicParameters();
        parameters.Add("SearchTerm", normalizedSearch);
        parameters.Add("SearchPattern", normalizedSearch is null ? null : $"%{normalizedSearch}%");
        parameters.Add("DateFrom", normalizedDateFrom);
        parameters.Add("DateTo", normalizedDateTo);
        parameters.Add("PageSize", normalizedPageSize);
        parameters.Add("Offset", offset);

        var sql = PaymentReceiptSql.SearchPaymentReceipts(sortBy, orderBy);
        var paymentReceipts = await connection.QueryAsync<PaymentReceipt>(
            sql,
            parameters);

        var total = await connection.ExecuteScalarAsync<int>(
            PaymentReceiptSql.CountPaymentReceipts,
            parameters);

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
}
