using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IPaymentReceiptCreatePageService
{
    Task<PaymentReceiptCreateViewModel> BuildAsync(string? invoiceNumber = null);
    Task PopulateInvoiceSummaryAsync(PaymentReceiptFormViewModel viewModel);
}
