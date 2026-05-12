namespace DocumentTracker.Repositories;

public static class PaymentReceiptSql
{
    public const string GetById = """
        SELECT
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
        FROM payment_receipts
        WHERE id = @Id;
        """;
    
    public const string GetProductsByReceiptId = """
        SELECT
            prp.payment_receipt_id AS PaymentReceiptId,
            prp.product_id AS ProductId,
            prp.quantity AS Quantity,
            p.id AS Id,
            p.name AS Name,
            p.unit_price AS UnitPrice,
            p.created_at_utc AS CreatedAtUtc,
            p.updated_at_utc AS UpdatedAtUtc
        FROM payment_receipt_products prp
        INNER JOIN products p ON p.id = prp.product_id
        WHERE prp.payment_receipt_id = @PaymentReceiptId
        ORDER BY p.name ASC;
        """;
}


