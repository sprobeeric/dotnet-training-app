using Dapper;
using DocumentTracker.Models;
using Npgsql;

namespace DocumentTracker.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DocumentRepository> _logger;

    public InvoiceRepository(NpgsqlDataSource dataSource, ILogger<DocumentRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<Invoice?> GetByIdAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Invoice>(InvoiceSql.GetById, new { Id = id });
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Invoice>(
            InvoiceSql.GetByInvoiceNumber,
            new { InvoiceNumber = invoiceNumber.Trim() });
    }

    public async Task<bool> UpdateAsync(Invoice invoice)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var rows = await connection.ExecuteAsync(
            InvoiceSql.UpdateInvoice,
            ToParameters(invoice),
            transaction);

        await transaction.CommitAsync();
        _logger.LogInformation("Updated Invoice with id {InvoiceId}. Rows affected: {RowsAffected}.", invoice.Id, rows);
        return rows == 1;
    }

    private static object ToParameters(Invoice invoice)
    {
        return new
        {
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerName,
            InvoiceDate = invoice.InvoiceDate.ToDateTime(TimeOnly.MinValue),
            DueDate = invoice.DueDate.ToDateTime(TimeOnly.MinValue),
            Status = invoice.Status.ToString(),
            invoice.Subtotal,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.Notes,
            invoice.CreatedAtUtc,
            invoice.UpdatedAtUtc
        };
    }
}
