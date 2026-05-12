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

    public const string SelectColumns = """
        id AS Id,
        receipt_number AS ReceiptNumber,
        payment_date_utc AS PaymentDateUtc,
        reference_number AS ReferenceNumber,
        total_amount AS TotalAmount,
        received AS Received,
        change_amount AS ChangeAmount,
        created_at_utc AS CreatedAtUtc,
        updated_at_utc AS UpdatedAtUtc,
        deleted_at_utc AS DeletedAtUtc
        """;

    public static string SearchPaymentReceipts(string sortBy, string orderBy) => $"""
        SELECT {SelectColumns}
        FROM payment_receipts
        WHERE deleted_at_utc IS NULL
        AND (
            @SearchTerm IS NULL
            OR receipt_number ILIKE @SearchPattern
            OR reference_number ILIKE @SearchPattern
        )
        AND (
            @DateFrom::timestamp IS NULL
            OR payment_date_utc >= @DateFrom::timestamp
        )
        AND (
            @DateTo::timestamp IS NULL
            OR payment_date_utc < @DateTo::timestamp
        )
        ORDER BY {sortBy} {orderBy}, id DESC
        LIMIT @PageSize
        OFFSET @Offset;
        """;

    public const string CountPaymentReceipts = """
        SELECT COUNT(*)
        FROM payment_receipts
        WHERE deleted_at_utc IS NULL
        AND (
            @SearchTerm IS NULL
            OR receipt_number ILIKE @SearchPattern
            OR reference_number ILIKE @SearchPattern
        )
        AND (
            @DateFrom::timestamp IS NULL
            OR payment_date_utc >= @DateFrom::timestamp
        )
        AND (
            @DateTo::timestamp IS NULL
            OR payment_date_utc < @DateTo::timestamp
        );
        """;
}
