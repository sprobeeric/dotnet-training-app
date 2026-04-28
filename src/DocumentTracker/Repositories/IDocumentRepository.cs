using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IDocumentRepository
{
    Task<IReadOnlyList<Document>> SearchAsync(string? searchTerm);
    Task<Document?> GetByIdAsync(int id);
    Task<Document?> GetByDocumentNumberAsync(string documentNumber);
    Task<int> CreateAsync(Document document);
    Task<bool> UpdateAsync(Document document);
    Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc);
}
