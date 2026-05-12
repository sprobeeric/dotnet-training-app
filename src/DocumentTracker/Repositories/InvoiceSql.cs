namespace DocumentTracker.Repositories;

public static class InvoiceSql
{
    public const string SelectColumns = """
        id AS Id,
        invoice_number AS InvoiceNumber,
        customer_name AS CustomerName,
        invoice_date AS InvoiceDate,
        due_date AS DueDate,
        status AS Status,
        subtotal AS Subtotal,
        tax_amount AS TaxAmount,
        total_amount AS TotalAmount,
        notes AS Notes,
        created_at_utc AS CreatedAtUtc,
        updated_at_utc AS UpdatedAtUtc,
        deleted_at_utc AS DeletedAtUtc
        """;

        public const string GetByInvoiceNumber = $"""
        SELECT {SelectColumns}
        FROM invoices
        WHERE lower(invoice_number) = lower(@InvoiceNumber)
        LIMIT 1;
        """; 

    public const string InsertInvoice = """
        INSERT INTO invoices
            (invoice_number, customer_name, invoice_date, due_date, status, subtotal, tax_amount, total_amount, notes, created_at_utc, updated_at_utc)
        VALUES
            (@InvoiceNumber, @CustomerName, @InvoiceDate, @DueDate, @Status, @Subtotal, @TaxAmount, @TotalAmount, @Notes, @CreatedAtUtc, @UpdatedAtUtc)
        RETURNING id;
        """;
}

