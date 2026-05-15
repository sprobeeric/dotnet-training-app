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

    public const string InvoiceNumberExists = """
        SELECT EXISTS (
            SELECT 1
            FROM invoices
            WHERE lower(invoice_number) = lower(@InvoiceNumber)
                AND (@ExcludeId IS NULL or id <> @ExcludeId)
            )
        """;

    public const string InsertInvoice = """
        INSERT INTO invoices
            (invoice_number, customer_name, invoice_date, due_date, status, subtotal, tax_amount, total_amount, notes, created_at_utc, updated_at_utc)
        VALUES
            (@InvoiceNumber, @CustomerName, @InvoiceDate, @DueDate, @Status, @Subtotal, @TaxAmount, @TotalAmount, @Notes, @CreatedAtUtc, @UpdatedAtUtc)
        RETURNING id;
        """;

    private const string SearchFilters = """
        FROM invoices
        WHERE deleted_at_utc IS NULL
            AND (
                @SearchTerm IS NULL
                OR invoice_number ILIKE @SearchPattern
                OR customer_name ILIKE @SearchPattern
                OR status ILIKE @SearchPattern
            )
            AND (@InvoiceDateFrom IS NULL OR invoice_date >= @InvoiceDateFrom)
            AND (@InvoiceDateTo IS NULL OR invoice_date <= @InvoiceDateTo)
        """;
    public const string CountInvoices = $"""
        SELECT COUNT(*)
        {SearchFilters};
        """;
    public static string SearchInvoicesPage(string orderByClause) => $"""
        SELECT {SelectColumns}
        {SearchFilters}
        ORDER BY {orderByClause}
        LIMIT @PageSize OFFSET @Offset;
        """;

    public const string SoftDeleteInvoice = """
        UPDATE invoices
        SET deleted_at_utc = @DeletedAtUtc,
            updated_at_utc = @DeletedAtUtc
        WHERE id = @Id
          AND deleted_at_utc IS NULL;
        """;
}
