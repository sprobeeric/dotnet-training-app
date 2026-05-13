using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IPaymentReceiptRepository
{
    Task<PaginatedResult<PaymentReceipt>> SearchAsync(string? searchTerm, DateOnly? dateFrom, DateOnly? dateTo, string sort, string order, int page, int pageSize);
    Task<PaymentReceipt?> GetByIdAsync(int id);
    Task<bool> ReceiptNumberExistsAsync(string receiptNumber, int? excludeId = null);
    Task<bool> ReferenceNumberExistsAsync(string referenceNumber, int? excludeId = null);
    Task<bool> InvoiceExistsAndPendingAsync(int invoiceId);
    Task<int> CreateAsync(PaymentReceipt paymentReceipt);
}
