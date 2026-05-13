using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IPaymentReceiptRepository
{
    Task<PaginatedResult<PaymentReceipt>> SearchAsync(PaymentReceiptSearchCriteria criteria);
    Task<PaymentReceipt?> GetByIdAsync(int id);
    Task<int> GetNextReceiptSequenceAsync(DateOnly paymentDate);
    Task<int> CreateAsync(PaymentReceipt paymentReceipt, IReadOnlyList<PaymentReceiptProduct> products);
    Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc);
}
