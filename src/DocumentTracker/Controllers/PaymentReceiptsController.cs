using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class PaymentReceiptsController : Controller
{
    private readonly IPaymentReceiptService _paymentReceiptService;
    private readonly ICurrentUserRoleProvider _roleProvider;
    private readonly ILogger<PaymentReceiptsController> _logger;

    public PaymentReceiptsController(
        IPaymentReceiptService paymentReceiptService,
        ICurrentUserRoleProvider roleProvider,
        ILogger<PaymentReceiptsController> logger)
    {
        _paymentReceiptService = paymentReceiptService;
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
    
    public async Task<IActionResult> Create()
    {
        return View(await _paymentReceiptService.GetCreateAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentReceiptCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var createViewModel = await _paymentReceiptService.GetCreateAsync();
            createViewModel.Received = viewModel.Received;
            MergeSubmittedQuantities(createViewModel, viewModel);
            return View(createViewModel);
        }

        var result = await _paymentReceiptService.CreateAsync(viewModel);
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
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

            var document = await _paymentReceiptService.GetDeleteAsync(id);
            return document is null ? View("ErrorMessage") : View(document);
        }

        return RedirectToAction(nameof(Index));
    }

    private static void MergeSubmittedQuantities(PaymentReceiptCreateViewModel target, PaymentReceiptCreateViewModel source)
    {
        var quantitiesByProductId = source.Products.ToDictionary(product => product.ProductId, product => product.Quantity);

        foreach (var product in target.Products)
        {
            if (quantitiesByProductId.TryGetValue(product.ProductId, out var quantity))
            {
                product.Quantity = quantity;
            }
        }
    }

    private void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
