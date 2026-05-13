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
        pr.payment_method AS PaymentMethod,
        pr.reference_number AS ReferenceNumber,
        pr.notes AS Notes,
        pr.created_at_utc AS CreatedAtUtc,
        pr.updated_at_utc AS UpdatedAtUtc,
        pr.deleted_at_utc AS DeletedAtUtc
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
        WHERE pr.id = @Id
          AND pr.deleted_at_utc IS NULL;
        """;
}
