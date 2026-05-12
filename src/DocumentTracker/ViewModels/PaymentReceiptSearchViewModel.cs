namespace DocumentTracker.ViewModels;

public class PaymentReceiptSearchViewModel
{
    public string? SearchTerm { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string Sort { get; set; } = "payment_date_utc";
    public string Order { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int Total { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public IReadOnlyList<PaymentReceiptListItemViewModel> PaymentReceipts { get; set; } = [];
}
