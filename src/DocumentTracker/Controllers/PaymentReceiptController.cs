using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class PaymentReceiptController : Controller
{
    private readonly IPaymentReceiptService _paymentReceiptService;

    public PaymentReceiptController(IPaymentReceiptService paymentReceiptService)
    {
        _paymentReceiptService = paymentReceiptService;
    }
    public async Task<IActionResult> Index(
        string? searchTerm,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sort = "payment_date_utc",
        string order = "desc",
        int page = 1,
        int pageSize = 10)
    {
        var paymentReceipts = await _paymentReceiptService.SearchAsync(
            searchTerm,  
            dateFrom,  
            dateTo,
            sort,
            order,
            page,
            pageSize
        );

        return View(new PaymentReceiptSearchViewModel
        {
            SearchTerm = searchTerm,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Sort = sort,
            Order = order,
            Page = page,
            PageSize = pageSize,
            Total = paymentReceipts.Total,
            PaymentReceipts = paymentReceipts.Items
        });
    }
}
