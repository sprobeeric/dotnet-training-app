using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IInvoiceLookupRepository
{
    Task<InvoicePaymentSummary?> GetPaymentReceiptSummaryByNumberAsync(string invoiceNumber);
}
