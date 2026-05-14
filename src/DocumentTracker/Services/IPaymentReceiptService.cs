using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IPaymentReceiptService
{
    Task<PaginatedResult<PaymentReceiptListItemViewModel>> SearchAsync(PaymentReceiptSearchViewModel model);
    Task<ServiceResult<int>> CreateAsync(PaymentReceiptCreateViewModel viewModel);
    Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id);
    Task<PaymentReceiptDeleteViewModel?> GetDeleteAsync(int id);
    Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole);
}
