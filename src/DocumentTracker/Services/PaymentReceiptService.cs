using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public class PaymentReceiptService: IPaymentReceiptService
{
    private readonly IPaymentReceiptRepository _repository;

    public PaymentReceiptService(IPaymentReceiptRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id)
    {
        var receipt = await _repository.GetByIdAsync(id);
        return receipt is null || receipt.DeletedAtUtc is not null
            ? null
            : ToDetails(receipt);
    }

    private static PaymentReceiptDetailsViewModel ToDetails(PaymentReceipt receipt) => new()
    {
        Id = receipt.Id,
        ReceiptNumber = receipt.ReceiptNumber,
        PaymentDateUtc = receipt.PaymentDateUtc,
        ReferenceNumber = receipt.ReferenceNumber,
        TotalAmount = receipt.TotalAmount,
        Received = receipt.Received,
        ChangeAmount = receipt.ChangeAmount,
        CreatedAtUtc = receipt.CreatedAtUtc,
        UpdatedAtUtc = receipt.UpdatedAtUtc,
        Products = receipt.PaymentReceiptProducts.Select(ToProductDetails).ToList()
    };

    private static PaymentReceiptProductDetailsViewModel ToProductDetails(PaymentReceiptProduct item) => new()
    {
        ProductId = item.ProductId,
        ProductName = item.Product.Name,
        UnitPrice = item.Product.UnitPrice,
        Quantity = item.Quantity
    };
}