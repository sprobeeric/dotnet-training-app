using DocumentTracker.Models;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IPaymentReceiptValidator
{
    ServiceResult<int> ValidateCreate(
        PaymentReceiptCreateViewModel viewModel,
        IReadOnlyList<PaymentReceiptProduct> selectedProducts);
}
