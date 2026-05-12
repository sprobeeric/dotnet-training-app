using DocumentTracker.Models;

namespace DocumentTracker.Repositories;

public interface IPaymentReceiptRepository
{
    Task<PaymentReceipt?> GetByIdAsync(int id);
}



