namespace DocumentTracker.ViewModels;

public class InvoiceSearchViewModel
{
    public string? SearchTerm { get; set; }
    public DateOnly? InvoiceDateFrom { get; set; }
    public DateOnly? InvoiceDateTo { get; set; }
    public IReadOnlyList<InvoiceListItemViewModel> Invoices { get; set; } = [];
}