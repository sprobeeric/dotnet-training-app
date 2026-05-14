using DocumentTracker.Models;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemViewModel>> SearchAsync(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize);

    Task<InvoiceDeleteViewModel?> GetDeleteAsync(int id);
    Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole);
}
