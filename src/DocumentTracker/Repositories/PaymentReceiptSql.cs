namespace DocumentTracker.Repositories;

public static class PaymentReceiptSql
{
    public const string GetNextReceiptSequence = """
        SELECT COALESCE(MAX(CAST(RIGHT(receipt_number, 6) AS integer)), 0) + 1
        FROM payment_receipts
        WHERE receipt_number LIKE @ReceiptPrefix || '-%';
        """;

    public const string InsertPaymentReceipt = """
        INSERT INTO payment_receipts
            (receipt_number, payment_date_utc, reference_number, total_amount, received, change_amount, created_at_utc, updated_at_utc)
        VALUES
            (@ReceiptNumber, @PaymentDateUtc, @ReferenceNumber, @TotalAmount, @Received, @ChangeAmount, @CreatedAtUtc, @UpdatedAtUtc)
        RETURNING id;
        """;

    public const string InsertPaymentReceiptProduct = """
        INSERT INTO payment_receipt_products
            (payment_receipt_id, product_id, quantity, unit_price, line_total)
        VALUES
            (@PaymentReceiptId, @ProductId, @Quantity, @UnitPrice, @LineTotal);
        """;
}
