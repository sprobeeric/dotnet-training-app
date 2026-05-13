using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICurrentUserRoleProvider _roleProvider;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IInvoiceService invoiceService,
        ICurrentUserRoleProvider roleProvider,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _roleProvider = roleProvider;
        _logger = logger;
    }

    public async Task<IActionResult> Delete(int id)
    {
        var invoice = await _invoiceService.GetDeleteAsync(id);
        return invoice is null ? NotFound() : View(invoice);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _invoiceService.SoftDeleteAsync(id, _roleProvider.GetCurrentRole());
        if (!result.Succeeded)
        {
            _logger.LogWarning("Soft delete was rejected for invoice id {InvoiceId}.", id);
            AddServiceErrors(result);

            var invoice = await _invoiceService.GetDeleteAsync(id);
            return invoice is null ? View("ErrorMessage") : View(invoice);
        }

        return RedirectToAction(nameof(Index));
    }

    private void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
