using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICurrentUserRoleProvider _roleProvider;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        IInvoiceService invoiceService,
        ICurrentUserRoleProvider roleProvider,
        ILogger<InvoiceController> logger)
    {
        _invoiceService = invoiceService;
        _roleProvider = roleProvider;
        _logger = logger;
    }

    public IActionResult Create()
    {
        return View(new InvoiceCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceCreateViewModel viewModel)
    {
      if (!ModelState.IsValid)
      {
          return View(viewModel);
      }

      var result = await _invoiceService.CreateAsync(viewModel);
      if (!result.Succeeded)
      {
          AddServiceErrors(result);
          return View(viewModel);
      }

      return RedirectToAction(nameof(Create));
    }

    private void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
