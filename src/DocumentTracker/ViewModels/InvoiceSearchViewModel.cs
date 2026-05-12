namespace DocumentTracker.ViewModels;

public class InvoiceSearchViewModel
{
    public string? SearchTerm { get; set; }
    public IReadOnlyList<InvoiceListItemViewModel> Invoices { get; set; } = [];
}