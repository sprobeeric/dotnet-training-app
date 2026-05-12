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

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Invoice>(
            InvoiceSql.GetByInvoiceNumber,
            new { InvoiceNumber = invoiceNumber.Trim() });
    }

    public async Task<int> CreateAsync(Invoice invoice)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var id = await connection.ExecuteScalarAsync<int>(
            InvoiceSql.InsertInvoice,
            ToParameters(invoice),
            transaction);

        await transaction.CommitAsync();
        _logger.LogInformation("Created invoice with id {InvoiceId}.", id);
        return id;
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

