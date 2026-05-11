namespace DocumentTracker.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public ICollection<PaymentReceiptProduct> PaymentReceiptProducts { get; set; } = new List<PaymentReceiptProduct>();
}