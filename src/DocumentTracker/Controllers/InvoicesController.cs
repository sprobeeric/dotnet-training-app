using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index(string? searchTerm, DateOnly? invoiceDateFrom, DateOnly? invoiceDateTo)
    {
        var invoices = await _invoiceService.SearchAsync(searchTerm, invoiceDateFrom, invoiceDateTo);
        return View(new InvoiceSearchViewModel
        {
            SearchTerm = searchTerm,
            InvoiceDateFrom = invoiceDateFrom,
            InvoiceDateTo = invoiceDateTo,
            Invoices = invoices
        });
    }
}