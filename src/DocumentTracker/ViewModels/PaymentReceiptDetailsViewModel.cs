namespace DocumentTracker.ViewModels;

public class PaymentReceiptDetailsViewModel
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime PaymentDateUtc { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal Received { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public IReadOnlyList<PaymentReceiptProductDetailsViewModel> Products { get; set; } = [];
}