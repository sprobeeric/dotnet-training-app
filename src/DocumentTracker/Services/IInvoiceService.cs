using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IInvoiceService
{
    Task<ServiceResult<InvoiceEditViewModel>> GetEditAsync(int id); 
    Task<ServiceResult> UpdateAsync(InvoiceEditViewModel viewModel);
}
