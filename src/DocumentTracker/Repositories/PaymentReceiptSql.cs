namespace DocumentTracker.Repositories;

public static class PaymentReceiptSql
{
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

    public const string ProductSelectColumns = """
        prp.payment_receipt_id AS PaymentReceiptId,
        prp.product_id AS ProductId,
        prp.quantity AS Quantity,
        prp.unit_price AS UnitPrice,
        prp.line_total AS LineTotal,
        p.id AS Id,
        p.name AS Name,
        p.unit_price AS UnitPrice,
        p.created_at_utc AS CreatedAtUtc,
        p.updated_at_utc AS UpdatedAtUtc,
        p.deleted_at_utc AS DeletedAtUtc
        """;

    public const string GetById = $"""
        SELECT {SelectColumns}
        FROM payment_receipts
        WHERE id = @Id
          AND deleted_at_utc IS NULL;
        """;

    public const string ListProductsByReceiptId = $"""
        SELECT {ProductSelectColumns}
        FROM payment_receipt_products prp
        INNER JOIN products p ON p.id = prp.product_id
        WHERE prp.payment_receipt_id = @PaymentReceiptId
        ORDER BY p.name ASC, p.id ASC;
        """;

    public const string GetNextReceiptSequence = """
        SELECT COALESCE(MAX(CAST(RIGHT(receipt_number, 6) AS integer)), 0) + 1
        FROM payment_receipts
        WHERE receipt_number LIKE @ReceiptPrefix || '-%'
          AND deleted_at_utc IS NULL;
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
