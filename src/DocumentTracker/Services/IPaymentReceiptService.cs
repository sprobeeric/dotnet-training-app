using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IPaymentReceiptService
{
    Task<PaymentReceiptCreateViewModel> GetCreateAsync();
    Task<ServiceResult<int>> CreateAsync(PaymentReceiptCreateViewModel viewModel);
}
