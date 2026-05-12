using Dapper;
using DocumentTracker.Models;
using Npgsql;

namespace DocumentTracker.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<InvoiceRepository> _logger;

    public InvoiceRepository(NpgsqlDataSource dataSource, ILogger<InvoiceRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<PagedResult<Invoice>> SearchAsync(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var offset = (pageNumber - 1) * pageSize;
        var orderByClause = GetOrderByClause(sortBy, sortDirection);

        await using var connection = await _dataSource.OpenConnectionAsync();
        using var results = await connection.QueryMultipleAsync(
            $"{InvoiceSql.CountInvoices}\n{InvoiceSql.SearchInvoicesPage(orderByClause)}",
            new
            {
                SearchTerm = normalizedSearch,
                SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%",
                InvoiceDateFrom = invoiceDateFrom,
                InvoiceDateTo = invoiceDateTo,
                PageSize = pageSize,
                Offset = offset
            });

        var totalCount = await results.ReadSingleAsync<int>();
        var invoices = (await results.ReadAsync<Invoice>()).AsList();

        _logger.LogDebug(
            "Searched invoices with term {SearchTerm}, invoice date from {InvoiceDateFrom}, invoice date to {InvoiceDateTo}, sort by {SortBy}, sort direction {SortDirection}, page {PageNumber}, page size {PageSize}.",
            normalizedSearch,
            invoiceDateFrom,
            invoiceDateTo,
            sortBy,
            sortDirection,
            pageNumber,
            pageSize);

        return new PagedResult<Invoice>
        {
            Items = invoices,
            TotalCount = totalCount
        };
    }

    private static string GetOrderByClause(string? sortBy, string? sortDirection)
    {
        var normalizedDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";

        return (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "invoice-number" => $"invoice_number {normalizedDirection}, id {normalizedDirection}",
            "customer" => $"customer_name {normalizedDirection}, id {normalizedDirection}",
            "invoice-date" => $"invoice_date {normalizedDirection}, id {normalizedDirection}",
            "due-date" => $"due_date {normalizedDirection}, id {normalizedDirection}",
            "status" => $"status {normalizedDirection}, id {normalizedDirection}",
            "total" => $"total_amount {normalizedDirection}, id {normalizedDirection}",
            _ => "COALESCE(updated_at_utc, created_at_utc) DESC, id DESC"
        };
    }
}