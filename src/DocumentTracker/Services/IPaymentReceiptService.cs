using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IPaymentReceiptService
{
    Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id);

    Task<PaginatedResult<PaymentReceiptListItemViewModel>> SearchAsync(
        string? searchTerm, 
        DateTime? dateFrom, 
        DateTime? dateTo,
        string sort,
        string order,
        int page, 
        int pageSize);
}
