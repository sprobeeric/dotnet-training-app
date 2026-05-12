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

    public async Task<PaymentReceipt?> GetByIdAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<PaymentReceipt>(PaymentReceiptSql.GetById, new { Id = id });
    }

    public async Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var rows = await connection.ExecuteAsync(
            PaymentReceiptSql.SoftDeletePaymentReceipt,
            new { Id = id, DeletedAtUtc = deletedAtUtc },
            transaction);

        await transaction.CommitAsync();
        _logger.LogInformation("Soft deleted payment receipt with id {PaymentReceiptId}. Rows affected: {RowsAffected}.", id, rows);
        return rows == 1;
    }
}
