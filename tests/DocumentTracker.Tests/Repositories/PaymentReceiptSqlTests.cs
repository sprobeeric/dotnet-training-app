using DocumentTracker.Repositories;

namespace DocumentTracker.Tests.Repositories;

public class PaymentReceiptSqlTests
{
    [Fact]
    public void SoftDeletePaymentReceipt_UpdatesDeletedAtUtcAndDoesNotPhysicallyDelete()
    {
        Assert.Contains("@DeletedAtUtc", PaymentReceiptSql.SoftDeletePaymentReceipt);
        Assert.Contains("deleted_at_utc IS NULL", PaymentReceiptSql.SoftDeletePaymentReceipt);
        Assert.DoesNotContain("DELETE FROM payment_receipts", PaymentReceiptSql.SoftDeletePaymentReceipt, StringComparison.OrdinalIgnoreCase);
    }
}
