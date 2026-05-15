using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
    Task<bool> UpdateAsync(Invoice invoice);
    Task<PagedResult<Invoice>> SearchAsync(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize);
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, int? excludeId = null);
    Task<int> CreateAsync(Invoice invoice);
    Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc);
}
