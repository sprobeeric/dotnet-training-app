namespace DocumentTracker.ViewModels;

public class InvoiceSearchViewModel
{
    public string? SearchTerm { get; set; }
    public DateOnly? InvoiceDateFrom { get; set; }
    public DateOnly? InvoiceDateTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public IReadOnlyList<InvoiceListItemViewModel> Invoices { get; set; } = [];

    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int FirstItemIndex => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int LastItemIndex => TotalCount == 0 ? 0 : Math.Min(PageNumber * PageSize, TotalCount);
}