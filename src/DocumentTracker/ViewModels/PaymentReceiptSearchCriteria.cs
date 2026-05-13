namespace DocumentTracker.Models;

public class PaymentReceiptSearchCriteria
{
    public string? SearchTerm { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string Sort { get; set; } = "payment_date";
    public string Order { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}