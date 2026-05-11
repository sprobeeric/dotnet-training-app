namespace DocumentTracker.Models;

public class PaymentReceiptProduct
{
    public int PaymentReceiptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public PaymentReceipt PaymentReceipt { get; set; } = null!;
    public Product Product { get; set; } = null!;
}