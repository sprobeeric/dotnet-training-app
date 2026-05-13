using DocumentTracker.Models;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IInvoiceService
{
    Task<ServiceResult<int>> CreateAsync(InvoiceCreateViewModel viewModel);
    Task<PagedResult<InvoiceListItemViewModel>> SearchAsync(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize);
}
