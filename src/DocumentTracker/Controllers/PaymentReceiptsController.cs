using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class PaymentReceiptsController : Controller
{
    private readonly IPaymentReceiptService _paymentReceiptService;
    private readonly IPaymentReceiptCreatePageService _createPageService;
    private readonly ICurrentUserRoleProvider _roleProvider;
    private readonly ILogger<PaymentReceiptsController> _logger;

    public PaymentReceiptsController(
        IPaymentReceiptService paymentReceiptService,
        IPaymentReceiptCreatePageService createPageService,
        ICurrentUserRoleProvider roleProvider,
        ILogger<PaymentReceiptsController> logger)
    {
        _paymentReceiptService = paymentReceiptService;
        _createPageService = createPageService;
        _roleProvider = roleProvider;
        _logger = logger;
    }

    public async Task<IActionResult> Index(PaymentReceiptSearchViewModel model)
    {
        var normalizedPage = model.Page < 1 ? 1 : model.Page;
        var normalizedPageSize = model.PageSize < 1 ? 10 : model.PageSize;

        var receipts = await _paymentReceiptService.SearchAsync(model);

        return View(new PaymentReceiptSearchViewModel
        {
            SearchTerm = model.SearchTerm,
            DateFrom = model.DateFrom,
            DateTo = model.DateTo,
            Sort = model.Sort,
            Order = model.Order,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            Total = receipts.Total,
            Receipts = receipts.Items
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

    public async Task<IActionResult> Delete(int id)
    {
        var receipt = await _paymentReceiptService.GetDeleteAsync(id);
        return receipt is null ? NotFound() : View(receipt);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _paymentReceiptService.SoftDeleteAsync(id, _roleProvider.GetCurrentRole());
        if (!result.Succeeded)
        {
            _logger.LogWarning("Soft delete was rejected for payment receipt id {PaymentReceiptId}.", id);
            AddServiceErrors(result);

            var receipt = await _paymentReceiptService.GetDeleteAsync(id);
            return receipt is null ? View("ErrorMessage") : View(receipt);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, string? invoiceNumber)
    {
        var result = await _paymentReceiptService.GetEditAsync(id);
        if (!result.Succeeded || result.Value is null)
        {
            AddServiceErrors(result);
            return View("ErrorMessage");
        }

        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            result.Value.InvoiceNumber = invoiceNumber.Trim();
            await _createPageService.PopulateInvoiceSummaryAsync(result.Value);
        }

        return View(result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PaymentReceiptEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await _createPageService.PopulateInvoiceSummaryAsync(viewModel);
            return View(viewModel);
        }

        var result = await _paymentReceiptService.UpdateAsync(viewModel);
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            await _createPageService.PopulateInvoiceSummaryAsync(viewModel);
            return View(viewModel);
        }

        TempData["SuccessMessage"] = "Payment receipt updated.";
        return RedirectToAction(nameof(Details), new { id = viewModel.Id });
    }

    private void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
