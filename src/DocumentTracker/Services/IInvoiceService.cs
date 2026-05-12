using DocumentTracker.Models;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemViewModel>> SearchAsync(string? searchTerm, DateOnly? invoiceDateFrom, DateOnly? invoiceDateTo, int pageNumber, int pageSize);
}