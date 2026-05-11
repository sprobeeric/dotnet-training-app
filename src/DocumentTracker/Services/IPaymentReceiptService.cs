using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IPaymentReceiptService
{
    Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id);
}