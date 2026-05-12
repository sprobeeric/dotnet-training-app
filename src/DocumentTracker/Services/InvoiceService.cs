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

    public async Task<PagedResult<InvoiceListItemViewModel>> SearchAsync(
        string? searchTerm,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? sortBy,
        string? sortDirection,
        int pageNumber,
        int pageSize)
    {
        var searchResult = await _repository.SearchAsync(
            searchTerm,
            invoiceDateFrom,
            invoiceDateTo,
            sortBy,
            sortDirection,
            pageNumber,
            pageSize);
        return new PagedResult<InvoiceListItemViewModel>
        {
            Items = searchResult.Items.Select(ToListItem).ToList(),
            TotalCount = searchResult.TotalCount
        };
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