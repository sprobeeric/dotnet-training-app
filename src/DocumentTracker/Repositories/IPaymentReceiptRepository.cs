using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IPaymentReceiptRepository
{
    Task<int> GetNextReceiptSequenceAsync(DateOnly paymentDate);
    Task<int> CreateAsync(PaymentReceipt paymentReceipt, IReadOnlyList<PaymentReceiptProduct> products);
}
