using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public class PaymentReceiptService : IPaymentReceiptService
{
    private readonly IPaymentReceiptRepository _paymentReceiptRepository;

    public PaymentReceiptService(IPaymentReceiptRepository paymentReceiptRepository)
    {
        _paymentReceiptRepository = paymentReceiptRepository;
    }

    public async Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id)
    {
        var receipt = await _paymentReceiptRepository.GetByIdAsync(id);
        return receipt is null || receipt.DeletedAtUtc is not null
            ? null
            : ToDetails(receipt);
    }

    public async Task<PaginatedResult<PaymentReceiptListItemViewModel>> SearchAsync(
        string? searchTerm, 
        DateTime? dateFrom, 
        DateTime? dateTo,
        string sort,
        string order,
        int page,
        int pageSize)
    {
        var paymentReceipts = await _paymentReceiptRepository.SearchAsync(
            searchTerm, 
            dateFrom, 
            dateTo,
            sort,
            order,
            page, 
            pageSize);
        
        return new PaginatedResult<PaymentReceiptListItemViewModel>
        {
            Items = paymentReceipts.Items.Select(ToListItem).ToList(),
            Total = paymentReceipts.Total
        };
    }

    private static PaymentReceiptListItemViewModel ToListItem(PaymentReceipt paymentReceipt) => new()
    {
        Id = paymentReceipt.Id,
        ReceiptNumber = paymentReceipt.ReceiptNumber,
        ReferenceNumber = paymentReceipt.ReferenceNumber,
        PaymentDateUtc = paymentReceipt.PaymentDateUtc,
        TotalAmount = paymentReceipt.TotalAmount,
        Received = paymentReceipt.Received,
        ChangeAmount = paymentReceipt.ChangeAmount
    };

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
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity
    };
}
