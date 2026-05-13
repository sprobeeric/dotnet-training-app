using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class InvoicesController : Controller
{
    private const int PageSize = 5;
    private const string DefaultSortBy = "updated";
    private const string DefaultSortDirection = "desc";

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

    public async Task<IActionResult> Index(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int page = 1)
    {
        var currentPage = page < 1 ? 1 : page;
        var normalizedSortBy = string.IsNullOrWhiteSpace(sortBy) ? DefaultSortBy : sortBy.Trim();
        var normalizedSortDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : DefaultSortDirection;
        var searchResult = await _invoiceService.SearchAsync(
            searchTerm,
            invoiceDateFrom,
            invoiceDateTo,
            normalizedSortBy,
            normalizedSortDirection,
            currentPage,
            PageSize);


        return View(new InvoiceSearchViewModel
        {
            SearchTerm = searchTerm,
            InvoiceDateFrom = invoiceDateFrom,
            InvoiceDateTo = invoiceDateTo,
            SortBy = normalizedSortBy,
            SortDirection = normalizedSortDirection,
            PageNumber = currentPage,
            PageSize = PageSize,
            TotalCount = searchResult.TotalCount,
            Invoices = searchResult.Items
        });
    }

    public IActionResult Create()
    {
        var viewModel = new InvoiceCreateViewModel
        {
            InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
            DueDate = DateOnly.FromDateTime(DateTime.Today)
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
