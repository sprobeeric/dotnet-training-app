using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IInvoiceRepository
{
    Task<PagedResult<Invoice>> SearchAsync(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize);
}