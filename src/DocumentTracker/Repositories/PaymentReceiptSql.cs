namespace DocumentTracker.Repositories;

public static class PaymentReceiptSql
{
    public const string SelectColumns = """
        pr.id AS Id,
        pr.receipt_number AS ReceiptNumber,
        pr.invoice_id AS InvoiceId,
        i.invoice_number AS InvoiceNumber,
        i.customer_name AS CustomerName,
        pr.payment_date AS PaymentDate,
        pr.amount_paid AS AmountPaid,
        GREATEST(i.total_amount - (
            SELECT COALESCE(SUM(active_pr.amount_paid), 0)
            FROM payment_receipts active_pr
            WHERE active_pr.invoice_id = i.id
            AND active_pr.deleted_at_utc IS NULL
        ), 0) AS OutstandingBalance,
        pr.payment_method AS PaymentMethod,
        pr.reference_number AS ReferenceNumber,
        pr.notes AS Notes,
        pr.created_at_utc AS CreatedAtUtc,
        pr.updated_at_utc AS UpdatedAtUtc,
        pr.deleted_at_utc AS DeletedAtUtc
        """;

    public const string InsertPaymentReceipt = """
        INSERT INTO payment_receipts
            (receipt_number, invoice_id, payment_date, amount_paid, payment_method, reference_number, notes, created_at_utc, updated_at_utc)
        VALUES
            (@ReceiptNumber, @InvoiceId, @PaymentDate, @AmountPaid, @PaymentMethod, @ReferenceNumber, @Notes, @CreatedAtUtc, @UpdatedAtUtc)
        RETURNING id;
        """;

    public const string ReceiptNumberExists = """
        SELECT EXISTS (
            SELECT 1
            FROM payment_receipts
            WHERE receipt_number = @ReceiptNumber
            AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        );
        """;

    public const string ReferenceNumberExists = """
        SELECT EXISTS (
            SELECT 1
            FROM payment_receipts
            WHERE reference_number = @ReferenceNumber
            AND (@ExcludeId IS NULL OR id <> @ExcludeId)
        );
        """;

    public const string InvoiceExistsAndPending = """
        SELECT EXISTS (
            SELECT 1
            FROM invoices
            WHERE id = @InvoiceId
            AND deleted_at_utc IS NULL
            AND status = 'Pending'
        );
        """;

    public const string UpdateInvoiceStatusToPaid = """
        UPDATE invoices i
        SET status = 'Paid',
            updated_at_utc = @UpdatedAtUtc
        WHERE i.id = @InvoiceId
        AND @AmountPaid >= i.total_amount - (
            SELECT COALESCE(SUM(pr.amount_paid), 0)
            FROM payment_receipts pr
            WHERE pr.invoice_id = i.id
            AND pr.deleted_at_utc IS NULL
            AND pr.id <> @PaymentReceiptId
        );
        """;

    public const string RecalculateInvoiceStatus = """
        UPDATE invoices i
        SET status = CASE
                WHEN COALESCE((
                    SELECT SUM(pr.amount_paid)
                    FROM payment_receipts pr
                    WHERE pr.invoice_id = i.id
                    AND pr.deleted_at_utc IS NULL
                ), 0) >= i.total_amount
                    THEN 'Paid'
                ELSE 'Pending'
            END,
            updated_at_utc = @UpdatedAtUtc
        WHERE i.id = @InvoiceId
        AND i.deleted_at_utc IS NULL
        AND i.status IN ('Pending', 'Paid');
        """;

    public static string SearchPaymentReceipts(string sortBy, string orderBy) => $"""
        SELECT {SelectColumns}
        FROM payment_receipts pr
        INNER JOIN invoices i
            ON i.id = pr.invoice_id
        WHERE pr.deleted_at_utc IS NULL
        AND (
            @SearchPattern::text IS NULL
            OR pr.receipt_number ILIKE @SearchPattern::text
            OR pr.reference_number ILIKE @SearchPattern::text
            OR i.invoice_number ILIKE @SearchPattern::text
            OR i.customer_name ILIKE @SearchPattern::text
            OR pr.payment_method ILIKE @SearchPattern::text
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
            @SearchPattern::text IS NULL
            OR pr.receipt_number ILIKE @SearchPattern::text
            OR pr.reference_number ILIKE @SearchPattern::text
            OR i.invoice_number ILIKE @SearchPattern::text
            OR i.customer_name ILIKE @SearchPattern::text
            OR pr.payment_method ILIKE @SearchPattern::text
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

    public const string SoftDeletePaymentReceipt = """
        UPDATE payment_receipts
        SET deleted_at_utc = @DeletedAtUtc,
            updated_at_utc = @DeletedAtUtc
        WHERE id = @Id
        AND deleted_at_utc IS NULL
        RETURNING invoice_id;
        """;

    public const string GetInvoiceIdForReceipt = """
        SELECT invoice_id
        FROM payment_receipts
        WHERE id = @Id
        AND deleted_at_utc IS NULL;
        """;

    public const string UpdatePaymentReceipt = """
        UPDATE payment_receipts
        SET receipt_number = @ReceiptNumber,
            invoice_id = @InvoiceId,
            payment_date = @PaymentDate,
            amount_paid = @AmountPaid,
            payment_method = @PaymentMethod,
            reference_number = @ReferenceNumber,
            notes = @Notes,
            updated_at_utc = @UpdatedAtUtc
        WHERE id = @Id
        AND deleted_at_utc IS NULL;
        """;
}
