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

    public async Task<PagedResult<Invoice>> SearchAsync(string? searchTerm, DateOnly? invoiceDateFrom, DateOnly? invoiceDateTo, int pageNumber, int pageSize)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var offset = (pageNumber - 1) * pageSize;

        await using var connection = await _dataSource.OpenConnectionAsync();
        using var results = await connection.QueryMultipleAsync(
            $"{InvoiceSql.CountInvoices}\n{InvoiceSql.SearchInvoicesPage}",
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
            "Searched invoices with term {SearchTerm}, invoice date from {InvoiceDateFrom}, invoice date to {InvoiceDateTo}, page {PageNumber}, page size {PageSize}.",
            normalizedSearch,
            invoiceDateFrom,
            invoiceDateTo,
            pageNumber,
            pageSize);

        return new PagedResult<Invoice>
        {
            Items = invoices,
            TotalCount = totalCount
        };
    }
}