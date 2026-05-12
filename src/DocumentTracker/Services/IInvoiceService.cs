using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceListItemViewModel>> SearchAsync(string? searchTerm);
}