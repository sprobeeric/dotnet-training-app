using Dapper;
using DocumentTracker.Models;
using Npgsql;

namespace DocumentTracker.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DocumentRepository> _logger;

    public DocumentRepository(NpgsqlDataSource dataSource, ILogger<DocumentRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Document>> SearchAsync(string? searchTerm)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        await using var connection = await _dataSource.OpenConnectionAsync();
        var documents = await connection.QueryAsync<Document>(
            DocumentSql.SearchDocuments,
            new
            {
                SearchTerm = normalizedSearch,
                SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%"
            });

        return documents.AsList();
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Document>(DocumentSql.GetById, new { Id = id });
    }

    public async Task<Document?> GetByDocumentNumberAsync(string documentNumber)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Document>(
            DocumentSql.GetByDocumentNumber,
            new { DocumentNumber = documentNumber.Trim() });
    }

    public async Task<int> CreateAsync(Document document)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var id = await connection.ExecuteScalarAsync<int>(
            DocumentSql.InsertDocument,
            ToParameters(document),
            transaction);

        await transaction.CommitAsync();
        _logger.LogInformation("Created document with id {DocumentId}.", id);
        return id;
    }

    public async Task<bool> UpdateAsync(Document document)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var rows = await connection.ExecuteAsync(
            DocumentSql.UpdateDocument,
            ToParameters(document),
            transaction);

        await transaction.CommitAsync();
        _logger.LogInformation("Updated document with id {DocumentId}. Rows affected: {RowsAffected}.", document.Id, rows);
        return rows == 1;
    }

    public async Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var rows = await connection.ExecuteAsync(
            DocumentSql.SoftDeleteDocument,
            new { Id = id, DeletedAtUtc = deletedAtUtc },
            transaction);

        await transaction.CommitAsync();
        _logger.LogInformation("Soft deleted document with id {DocumentId}. Rows affected: {RowsAffected}.", id, rows);
        return rows == 1;
    }

    private static object ToParameters(Document document)
    {
        return new
        {
            document.Id,
            document.Title,
            document.DocumentNumber,
            document.Department,
            document.OwnerName,
            Status = document.Status.ToString(),
            document.Description,
            document.CreatedAtUtc,
            document.UpdatedAtUtc
        };
    }
}
