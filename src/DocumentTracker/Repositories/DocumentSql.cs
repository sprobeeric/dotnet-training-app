namespace DocumentTracker.Repositories;

public static class DocumentSql
{
    public const string SelectColumns = """
        id AS Id,
        title AS Title,
        document_number AS DocumentNumber,
        department AS Department,
        owner_name AS OwnerName,
        status AS Status,
        description AS Description,
        created_at_utc AS CreatedAtUtc,
        updated_at_utc AS UpdatedAtUtc,
        deleted_at_utc AS DeletedAtUtc
        """;

    public const string SearchDocuments = $"""
        SELECT {SelectColumns}
        FROM documents
        WHERE deleted_at_utc IS NULL
          AND (
              @SearchTerm IS NULL
              OR title ILIKE @SearchPattern
              OR document_number ILIKE @SearchPattern
              OR department ILIKE @SearchPattern
              OR owner_name ILIKE @SearchPattern
              OR status ILIKE @SearchPattern
          )
        ORDER BY updated_at_utc DESC, id DESC;
        """;

    public const string GetById = $"""
        SELECT {SelectColumns}
        FROM documents
        WHERE id = @Id;
        """;

    public const string GetByDocumentNumber = $"""
        SELECT {SelectColumns}
        FROM documents
        WHERE lower(document_number) = lower(@DocumentNumber)
        LIMIT 1;
        """;

    public const string InsertDocument = """
        INSERT INTO documents
            (title, document_number, department, owner_name, status, description, created_at_utc, updated_at_utc)
        VALUES
            (@Title, @DocumentNumber, @Department, @OwnerName, @Status, @Description, @CreatedAtUtc, @UpdatedAtUtc)
        RETURNING id;
        """;

    public const string UpdateDocument = """
        UPDATE documents
        SET title = @Title,
            document_number = @DocumentNumber,
            department = @Department,
            owner_name = @OwnerName,
            status = @Status,
            description = @Description,
            updated_at_utc = @UpdatedAtUtc
        WHERE id = @Id
          AND deleted_at_utc IS NULL;
        """;

    public const string SoftDeleteDocument = """
        UPDATE documents
        SET deleted_at_utc = @DeletedAtUtc,
            updated_at_utc = @DeletedAtUtc
        WHERE id = @Id
          AND deleted_at_utc IS NULL;
        """;
}
