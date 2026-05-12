using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;

    public InvoiceService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<InvoiceListItemViewModel>> SearchAsync(string? searchTerm)
    {
        var invoices = await _repository.SearchAsync(searchTerm);
        return invoices.Select(ToListItem).ToList();
    }

    private static InvoiceListItemViewModel ToListItem(Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        CustomerName = invoice.CustomerName,
        InvoiceDate = invoice.InvoiceDate,
        DueDate = invoice.DueDate,
        Status = invoice.Status,
        TotalAmount = invoice.TotalAmount,
        UpdatedAtUtc = invoice.UpdatedAtUtc
    };
}