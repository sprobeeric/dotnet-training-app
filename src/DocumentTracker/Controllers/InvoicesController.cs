using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class InvoicesController : Controller
{
    private const int PageSize = 5;
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index(string? searchTerm, DateOnly? invoiceDateFrom, DateOnly? invoiceDateTo, int page = 1)
    {
        var currentPage = page < 1 ? 1 : page;
        var searchResult = await _invoiceService.SearchAsync(searchTerm, invoiceDateFrom, invoiceDateTo, currentPage, PageSize);

        if (currentPage > 1 && searchResult.TotalCount > 0 && searchResult.Items.Count == 0)
        {
            currentPage = (int)Math.Ceiling(searchResult.TotalCount / (double)PageSize);
            searchResult = await _invoiceService.SearchAsync(searchTerm, invoiceDateFrom, invoiceDateTo, currentPage, PageSize);
        }

        return View(new InvoiceSearchViewModel
        {
            SearchTerm = searchTerm,
            InvoiceDateFrom = invoiceDateFrom,
            InvoiceDateTo = invoiceDateTo,
            PageNumber = currentPage,
            PageSize = PageSize,
            TotalCount = searchResult.TotalCount,
            Invoices = searchResult.Items
        });
    }
}