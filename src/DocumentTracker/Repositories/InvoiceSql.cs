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

    public const string GetById = $"""
        SELECT {SelectColumns}
        FROM invoices
        WHERE id = @Id;
        """;

    public const string GetByInvoiceNumber = $"""
        SELECT {SelectColumns}
        FROM invoices
        WHERE lower(invoice_number) = lower(@InvoiceNumber)
        LIMIT 1;
        """;

    public const string UpdateInvoice = """
        UPDATE invoices
        SET invoice_number = @InvoiceNumber,
            customer_name = @CustomerName,
            invoice_date = @InvoiceDate,
            due_date = @DueDate,
            status = @Status,
            subtotal = @Subtotal,
            tax_amount = @TaxAmount,
            total_amount = @TotalAmount,
            notes = @Notes,
            updated_at_utc = @UpdatedAtUtc
        WHERE id = @Id
        AND deleted_at_utc IS NULL;
        """;
}
