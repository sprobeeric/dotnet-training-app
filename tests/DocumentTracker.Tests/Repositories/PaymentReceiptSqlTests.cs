using DocumentTracker.Repositories;

namespace DocumentTracker.Tests.Repositories;

public class PaymentReceiptSqlTests
{
    [Fact]
    public void SearchPaymentReceipts_UsesParametersForSearchInput()
    {
        var sql = PaymentReceiptSql.SearchPaymentReceipts("pr.payment_date", "DESC");

        Assert.Contains("@SearchPattern", sql);
        Assert.Contains("@DateFrom", sql);
        Assert.Contains("@DateTo", sql);
        Assert.DoesNotContain("ILIKE '%", sql);
        Assert.DoesNotContain("string.Concat", sql);
    }

    [Fact]
    public void InsertAndDeleteSql_UseNamedParameters()
    {
        Assert.Contains("@ReceiptNumber", PaymentReceiptSql.InsertPaymentReceipt);
        Assert.Contains("@InvoiceId", PaymentReceiptSql.InsertPaymentReceipt);
        Assert.Contains("@DeletedAtUtc", PaymentReceiptSql.SoftDeletePaymentReceipt);
        Assert.DoesNotContain("DELETE FROM payment_receipts", PaymentReceiptSql.SoftDeletePaymentReceipt, StringComparison.OrdinalIgnoreCase);
    }
}
