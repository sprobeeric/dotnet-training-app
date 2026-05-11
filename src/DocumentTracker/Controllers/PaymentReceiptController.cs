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

    public async Task<IActionResult> Create()
    {
        return View(await _paymentReceiptService.GetCreateAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var paymentReceipt = await _paymentReceiptService.GetDetailsAsync(id);
        return paymentReceipt is null ? NotFound() : View(paymentReceipt);
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

        return RedirectToAction(nameof(Details), new { id = result.Value });
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
