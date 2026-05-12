using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public class PaymentReceiptService : IPaymentReceiptService
{
    private readonly IPaymentReceiptRepository _paymentReceiptRepository;
    private readonly ILogger<PaymentReceiptService> _logger;

    public PaymentReceiptService(
        IPaymentReceiptRepository paymentReceiptRepository,
        ILogger<PaymentReceiptService> logger)
    {
        _paymentReceiptRepository = paymentReceiptRepository;
        _logger = logger;
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
}