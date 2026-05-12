using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IInvoiceService
{
    Task<ServiceResult<int>> CreateAsync(InvoiceCreateViewModel viewModel);
}
