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
        Assert.Contains("@PageSize", sql);
        Assert.Contains("@Offset", sql);
        Assert.DoesNotContain("ILIKE '%", sql);
        Assert.DoesNotContain("string.Concat", sql);
    }

    [Fact]
    public void InsertAndDeleteSql_UseNamedParameters()
    {
        Assert.Contains("@ReceiptNumber", PaymentReceiptSql.InsertPaymentReceipt);
        Assert.Contains("@InvoiceId", PaymentReceiptSql.InsertPaymentReceipt);
        Assert.Contains("@AmountPaid", PaymentReceiptSql.InsertPaymentReceipt);
        Assert.Contains("@DeletedAtUtc", PaymentReceiptSql.SoftDeletePaymentReceipt);
        Assert.Contains("@Id", PaymentReceiptSql.SoftDeletePaymentReceipt);
        Assert.Contains("RETURNING invoice_id", PaymentReceiptSql.SoftDeletePaymentReceipt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM payment_receipts", PaymentReceiptSql.SoftDeletePaymentReceipt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvoiceStatusSql_UseNamedParameters()
    {
        Assert.Contains("@InvoiceId", PaymentReceiptSql.InvoiceExistsAndPending);
        Assert.Contains("@InvoiceId", PaymentReceiptSql.UpdateInvoiceStatusToPaid);
        Assert.Contains("@PaymentReceiptId", PaymentReceiptSql.UpdateInvoiceStatusToPaid);
        Assert.Contains("@AmountPaid", PaymentReceiptSql.UpdateInvoiceStatusToPaid);
        Assert.Contains("@UpdatedAtUtc", PaymentReceiptSql.UpdateInvoiceStatusToPaid);

        Assert.Contains("@InvoiceId", PaymentReceiptSql.RecalculateInvoiceStatusAfterReceiptDelete);
        Assert.Contains("@UpdatedAtUtc", PaymentReceiptSql.RecalculateInvoiceStatusAfterReceiptDelete);
        Assert.Contains("THEN 'Paid'", PaymentReceiptSql.RecalculateInvoiceStatusAfterReceiptDelete, StringComparison.Ordinal);
        Assert.Contains("ELSE 'Pending'", PaymentReceiptSql.RecalculateInvoiceStatusAfterReceiptDelete, StringComparison.Ordinal);
    }
}
