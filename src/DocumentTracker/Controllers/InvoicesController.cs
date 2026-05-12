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

    public IActionResult Create()
    {
        var viewModel = new InvoiceCreateViewModel
        {
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        };

        return View(viewModel);
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

       return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    private void AddServiceErrors(ServiceResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Key, error.Message);
        }
    }
}
