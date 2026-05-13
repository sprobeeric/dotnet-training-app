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

    public async Task<PaginatedResult<PaymentReceiptListItemViewModel>> SearchAsync(string? searchTerm, DateOnly? dateFrom, DateOnly? dateTo, string sort, string order, int page, int pageSize)
    {
        var paymentReceipts = await _paymentReceiptRepository.SearchAsync(searchTerm, dateFrom, dateTo, sort, order, page, pageSize);

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
        InvoiceNumber = paymentReceipt.InvoiceNumber,
        CustomerName = paymentReceipt.CustomerName,
        PaymentDate = paymentReceipt.PaymentDate,
        AmountPaid = paymentReceipt.AmountPaid,
        PaymentMethod = paymentReceipt.PaymentMethod,
        ReferenceNumber = paymentReceipt.ReferenceNumber,
        Notes = paymentReceipt.Notes
    };

    public async Task<PaymentReceiptDetailsViewModel?> GetDetailsAsync(int id)
    {
        var receipt = await _paymentReceiptRepository.GetByIdAsync(id);
        return receipt is null || receipt.DeletedAtUtc is not null
            ? null
            : ToDetails(receipt);
    }

    private static PaymentReceiptDetailsViewModel ToDetails(PaymentReceipt receipt) => new()
    {
        Id = receipt.Id,
        ReceiptNumber = receipt.ReceiptNumber,
        InvoiceId = receipt.InvoiceId,
        InvoiceNumber = receipt.InvoiceNumber,
        CustomerName = receipt.CustomerName,
        PaymentDate = receipt.PaymentDate,
        AmountPaid = receipt.AmountPaid,
        PaymentMethod = receipt.PaymentMethod,
        ReferenceNumber = receipt.ReferenceNumber,
        Notes = receipt.Notes,
        CreatedAtUtc = receipt.CreatedAtUtc,
        UpdatedAtUtc = receipt.UpdatedAtUtc
    };
}
