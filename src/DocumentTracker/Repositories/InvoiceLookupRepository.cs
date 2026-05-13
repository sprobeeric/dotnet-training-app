using Dapper;
using DocumentTracker.Models;
using Npgsql;

namespace DocumentTracker.Repositories;

public class InvoiceLookupRepository : IInvoiceLookupRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public InvoiceLookupRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<InvoicePaymentSummary?> GetPaymentReceiptSummaryByNumberAsync(string invoiceNumber)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();

        return await connection.QuerySingleOrDefaultAsync<InvoicePaymentSummary>(
            InvoiceLookupSql.GetPaymentReceiptSummaryByNumber,
            new { InvoiceNumber = invoiceNumber });
    }
}
