using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IPaymentReceiptRepository
{
    Task<PaginatedResult<PaymentReceipt>> SearchAsync(PaymentReceiptSearchCriteria criteria);
    Task<PaymentReceipt?> GetByIdAsync(int id);
    Task<bool> ReceiptNumberExistsAsync(string receiptNumber, int? excludeId = null);
    Task<bool> ReferenceNumberExistsAsync(string referenceNumber, int? excludeId = null);
    Task<bool> InvoiceExistsAndPendingAsync(int invoiceId);
    Task<int> CreateAsync(PaymentReceipt paymentReceipt);
    Task<bool> SoftDeleteAsync(int id, DateTime deletedAtUtc);
    Task<bool> UpdateAsync(PaymentReceipt paymentReceipt);
}
