using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id);
    Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc);
}
