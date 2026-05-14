using DocumentTracker.Models;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IInvoiceService
{
    Task<ServiceResult<InvoiceEditViewModel>> GetEditAsync(int id); 
    Task<ServiceResult> UpdateAsync(InvoiceEditViewModel viewModel);
    Task<PagedResult<InvoiceListItemViewModel>> SearchAsync(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize);
    Task<ServiceResult<int>> CreateAsync(InvoiceCreateViewModel viewModel);

    Task<InvoiceDeleteViewModel?> GetDeleteAsync(int id);
    Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole);
}
