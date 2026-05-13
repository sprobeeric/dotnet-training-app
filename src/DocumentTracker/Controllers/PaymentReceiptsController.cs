using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class PaymentReceiptsController : Controller
{
    private readonly IPaymentReceiptService _paymentReceiptService;
    private readonly IPaymentReceiptCreatePageService _createPageService;

    public PaymentReceiptsController(
        IPaymentReceiptService paymentReceiptService,
        IPaymentReceiptCreatePageService createPageService)
    {
        _paymentReceiptService = paymentReceiptService;
        _createPageService = createPageService;
    }

    public async Task<IActionResult> Index(string? searchTerm, DateOnly? dateFrom, DateOnly? dateTo, string sort = "payment_date", string order = "desc", int page = 1, int pageSize = 10)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 10 : pageSize;

        var paymentReceipts = await _paymentReceiptService.SearchAsync(searchTerm, dateFrom, dateTo, sort, order, normalizedPage, normalizedPageSize);

        return View(new PaymentReceiptSearchViewModel
        {
            SearchTerm = searchTerm,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Sort = sort,
            Order = order,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            Total = paymentReceipts.Total,
            PaymentReceipts = paymentReceipts.Items
        });
    }
    
    public async Task<IActionResult> Create(string? invoiceNumber)
    {
        return View(await _createPageService.BuildAsync(invoiceNumber));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentReceiptCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await _createPageService.PopulateInvoiceSummaryAsync(viewModel);
            return View(viewModel);
        }

        var result = await _paymentReceiptService.CreateAsync(viewModel);
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            await _createPageService.PopulateInvoiceSummaryAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Payment receipt created.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    public async Task<IActionResult> Details(int id)
    {
        var receipt = await _paymentReceiptService.GetDetailsAsync(id);
        return receipt is null ? NotFound() : View(receipt);
    }

    private void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
