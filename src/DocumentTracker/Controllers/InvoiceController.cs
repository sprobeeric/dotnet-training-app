using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DocumentTracker.Controllers;

public class InvoiceController : Controller
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index(string? searchTerm)
    {
        var invoices = await _invoiceService.SearchAsync(searchTerm);
        return View(new InvoiceSearchViewModel
        {
            SearchTerm = searchTerm,
            Invoices = invoices
        });
    }
}
