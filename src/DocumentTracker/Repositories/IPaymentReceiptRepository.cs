using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IPaymentReceiptRepository
{
    Task<PaymentReceipt?> GetByIdAsync(int id);

    Task<PaginatedResult<PaymentReceipt>> SearchAsync(
        string? searchTerm,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sort,
        string order,
        int page, 
        int pageSize);
}
