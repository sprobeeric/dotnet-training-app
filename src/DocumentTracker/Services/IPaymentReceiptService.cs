using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IPaymentReceiptService
{
    Task<PaymentReceiptDeleteViewModel?> GetDeleteAsync(int id);
    Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole);
}
