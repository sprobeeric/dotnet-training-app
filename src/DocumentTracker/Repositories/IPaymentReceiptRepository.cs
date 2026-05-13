using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IPaymentReceiptRepository
{
    Task<PaginatedResult<PaymentReceipt>> SearchAsync(string? searchTerm, DateOnly? dateFrom, DateOnly? dateTo, string sort, string order, int page, int pageSize);
    Task<PaymentReceipt?> GetByIdAsync(int id);
    Task<int> GetNextReceiptSequenceAsync(DateOnly paymentDate);
    Task<int> CreateAsync(PaymentReceipt paymentReceipt, IReadOnlyList<PaymentReceiptProduct> products);
}
