using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IPaymentReceiptService
{
    Task<PaginatedResult<PaymentReceiptListItemViewModel>> SearchAsync(string? searchTerm, DateOnly? dateFrom, DateOnly? dateTo, string sort, string order, int page, int pageSize);
    Task<PaymentReceiptCreateViewModel> GetCreateAsync();
    Task<ServiceResult<int>> CreateAsync(PaymentReceiptCreateViewModel viewModel);
    Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id);
}
