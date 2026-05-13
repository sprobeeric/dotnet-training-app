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

        public const string GetById = $"""
        SELECT {SelectColumns}
        FROM invoices
        WHERE id = @Id;
        """;

    public const string SoftDeleteInvoice = """
        UPDATE invoices
        SET deleted_at_utc = @DeletedAtUtc,
            updated_at_utc = @DeletedAtUtc
        WHERE id = @Id
          AND deleted_at_utc IS NULL;
        """;
}