namespace DocumentTracker.Repositories;

public static class InvoiceLookupSql
{
    public const string GetPaymentReceiptSummaryByNumber = """
        SELECT
            i.id AS InvoiceId,
            i.invoice_number AS InvoiceNumber,
            i.customer_name AS CustomerName,
            i.invoice_date AS InvoiceDate,
            i.due_date AS DueDate,
            i.status AS Status,
            i.total_amount AS TotalAmount,
            COALESCE(SUM(pr.amount_paid), 0) AS PreviouslyPaid,
            i.notes AS Notes
        FROM invoices i
        LEFT JOIN payment_receipts pr
            ON pr.invoice_id = i.id
            AND pr.deleted_at_utc IS NULL
        WHERE i.deleted_at_utc IS NULL
        AND LOWER(i.invoice_number) = LOWER(@InvoiceNumber)
        GROUP BY i.id, i.invoice_number, i.customer_name, i.invoice_date, i.due_date, i.status, i.total_amount, i.notes;
        """;
}
