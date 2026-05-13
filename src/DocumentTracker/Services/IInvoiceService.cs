using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IInvoiceService
{
    Task<InvoiceDeleteViewModel?> GetDeleteAsync(int id);
    Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole);
}
