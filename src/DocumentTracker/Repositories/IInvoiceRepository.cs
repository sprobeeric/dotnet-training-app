using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByInvoiceNumberAsync(string documentNumber);
    Task<int> CreateAsync(Invoice invoice);
}
