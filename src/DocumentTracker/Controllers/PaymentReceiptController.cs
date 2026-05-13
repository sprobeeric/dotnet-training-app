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
