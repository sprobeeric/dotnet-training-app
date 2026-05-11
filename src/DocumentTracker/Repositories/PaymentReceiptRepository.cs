using Dapper;
using DocumentTracker.Models;
using Npgsql;

namespace DocumentTracker.Repositories;

public class PaymentReceiptRepository : IPaymentReceiptRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PaymentReceiptRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<PaymentReceipt?> GetByIdAsync (int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();

        var receipt = await connection.QuerySingleOrDefaultAsync<PaymentReceipt>(
            PaymentReceiptSql.GetById,
            new { Id = id });
        
        if (receipt is null)
        {
            return null;
        }

        var products = await connection.QueryAsync<PaymentReceiptProduct, Product, PaymentReceiptProduct>(
            PaymentReceiptSql.GetProductsByReceiptId,
            (receiptProduct, product) =>
            {
                receiptProduct.Product = product;
                return receiptProduct;
            },
            new { PaymentReceiptId = id },
            splitOn: "Id");

        receipt.PaymentReceiptProducts = products.AsList();
        return receipt;
    }
}