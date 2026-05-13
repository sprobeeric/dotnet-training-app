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

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Invoice>(InvoiceSql.GetById, new { Id = id });
    }

    public async Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var rows = await connection.ExecuteAsync(
            InvoiceSql.SoftDeleteInvoice,
            new { Id = id, DeletedAtUtc = deletedAtUtc },
            transaction);

        await transaction.CommitAsync();
        _logger.LogInformation("Soft deleted invoice with id {InvoiceId}. Rows affected: {RowsAffected}.", id, rows);
        return rows == 1;
    }
}
