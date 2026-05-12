using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IInvoiceRepository
{
    Task<IReadOnlyList<Invoice>> SearchAsync(string? searchTerm, DateOnly? invoiceDateFrom, DateOnly? invoiceDateTo);
}