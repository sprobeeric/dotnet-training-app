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

    public async Task<IReadOnlyList<Invoice>> SearchAsync(string? searchTerm)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        await using var connection = await _dataSource.OpenConnectionAsync();
        var invoices = await connection.QueryAsync<Invoice>(
            InvoiceSql.SearchInvoices,
            new
            {
                SearchTerm = normalizedSearch,
                SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%"
            });

        _logger.LogDebug("Searched invoices with term {SearchTerm}.", normalizedSearch);
        return invoices.AsList();
    }
}