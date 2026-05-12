using DocumentTracker.Services;
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

    public async Task<IActionResult> Delete(int id)
    {
        var paymentReceipt = await _paymentReceiptService.GetDeleteAsync(id);
        return paymentReceipt is null ? NotFound() : View(paymentReceipt);
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

            var paymentReceipt = await _paymentReceiptService.GetDeleteAsync(id);
            return paymentReceipt is null ? View("ErrorMessage") : View(paymentReceipt);
        }

        return RedirectToAction("Index", "Documents");
    }

    private void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
