using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public class PaymentReceiptService : IPaymentReceiptService
{
    private readonly IPaymentReceiptRepository _repository;

    public PaymentReceiptService(IPaymentReceiptRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentReceiptDeleteViewModel?> GetDeleteAsync(int id)
    {
        var paymentReceipt = await _repository.GetByIdAsync(id);
        return paymentReceipt is null || paymentReceipt.DeletedAtUtc is not null ? null : ToDelete(paymentReceipt);
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole)
    {
        if (!string.Equals(currentRole, DocumentRoles.DocumentAdmin, StringComparison.Ordinal))
        {
            return ServiceResult.Failure(string.Empty, "Only users in the DocumentAdmin role can delete payment receipts.");
        }

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult.Failure(string.Empty, "The payment receipt was not found.");
        }

        if (existing.DeletedAtUtc is not null)
        {
            return ServiceResult.Failure(string.Empty, "This payment receipt has already been deleted.");
        }

        var deleted = await _repository.SoftDeleteAsync(id, DateTime.UtcNow);
        return deleted
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Empty, "The payment receipt could not be deleted. It may have been changed by another user.");
    }

    private static PaymentReceiptDeleteViewModel ToDelete(PaymentReceipt paymentReceipt) => new()
    {
        Id = paymentReceipt.Id,
        ReceiptNumber = paymentReceipt.ReceiptNumber,
        PaymentDateUtc = paymentReceipt.PaymentDateUtc,
        ReferenceNumber = paymentReceipt.ReferenceNumber,
        TotalAmount = paymentReceipt.TotalAmount,
        Received = paymentReceipt.Received,
        ChangeAmount = paymentReceipt.ChangeAmount
    };
}
