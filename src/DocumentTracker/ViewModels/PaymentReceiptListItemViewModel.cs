namespace DocumentTracker.ViewModels;

public class PaymentReceiptListItemViewModel
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime PaymentDateUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Received { get; set; }
    public decimal ChangeAmount { get; set; }
}
