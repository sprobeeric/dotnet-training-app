namespace DocumentTracker.Repositories;

public static class PaymentReceiptSql
{
    public const string SelectColumns = """
        pr.id AS Id,
        pr.receipt_number AS ReceiptNumber,
        pr.invoice_id AS InvoiceId,
        i.invoice_number AS InvoiceNumber,
        pr.payment_date AS PaymentDate,
        pr.amount_paid AS AmountPaid,
        pr.payment_method AS PaymentMethod,
        pr.reference_number AS ReferenceNumber,
        pr.notes AS Notes,
        pr.created_at_utc AS CreatedAtUtc,
        pr.updated_at_utc AS UpdatedAtUtc,
        pr.deleted_at_utc AS DeletedAtUtc
        """;
    
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

    public static string SearchPaymentReceipts(string sortBy, string orderBy) => $"""
        SELECT {SelectColumns}
        FROM payment_receipts pr
        INNER JOIN invoices i
            ON i.id = pr.invoice_id
        WHERE pr.deleted_at_utc IS NULL
        AND (
            @SearchTerm IS NULL
            OR pr.receipt_number ILIKE @SearchPattern
            OR pr.reference_number ILIKE @SearchPattern
            OR i.invoice_number ILIKE @SearchPattern
        )
        AND (
            @DateFrom::date IS NULL
            OR pr.payment_date >= @DateFrom::date
        )
        AND (
            @DateTo::date IS NULL
            OR pr.payment_date <= @DateTo::date
        )
        ORDER BY {sortBy} {orderBy}, pr.id DESC
        LIMIT @PageSize
        OFFSET @Offset;
        """;

    public const string CountPaymentReceipts = """
        SELECT COUNT(*)
        FROM payment_receipts pr
        INNER JOIN invoices i
            ON i.id = pr.invoice_id
        WHERE pr.deleted_at_utc IS NULL
        AND (
            @SearchTerm IS NULL
            OR pr.receipt_number ILIKE @SearchPattern
            OR pr.reference_number ILIKE @SearchPattern
            OR i.invoice_number ILIKE @SearchPattern
        )
        AND (
            @DateFrom::date IS NULL
            OR pr.payment_date >= @DateFrom::date
        )
        AND (
            @DateTo::date IS NULL
            OR pr.payment_date <= @DateTo::date
        );
        """;

    public const string GetById = $"""
        SELECT {SelectColumns}
        FROM payment_receipts pr
        INNER JOIN invoices i
            ON i.id = pr.invoice_id
        WHERE pr.id = @Id;
        """;

    public const string GetProductsByReceiptId = """
        SELECT
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
        FROM payment_receipt_products prp
        INNER JOIN products p ON p.id = prp.product_id
        WHERE prp.payment_receipt_id = @PaymentReceiptId
        ORDER BY p.name ASC;
        """;

    public const string SoftDeletePaymentReceipt = """
        UPDATE payment_receipts
        SET deleted_at_utc = @DeletedAtUtc,
            updated_at_utc = @DeletedAtUtc
        WHERE id = @Id
          AND deleted_at_utc IS NULL;
        """;
}
