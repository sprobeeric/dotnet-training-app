namespace DocumentTracker.Repositories;

public static class ProductSql
{
    public const string SelectColumns = """
        id AS Id,
        name AS Name,
        unit_price AS UnitPrice,
        created_at_utc AS CreatedAtUtc,
        updated_at_utc AS UpdatedAtUtc,
        deleted_at_utc AS DeletedAtUtc
        """;

    public const string ListActiveProducts = $"""
        SELECT {SelectColumns}
        FROM products
        WHERE deleted_at_utc IS NULL
        ORDER BY name ASC, id ASC;
        """;
}
