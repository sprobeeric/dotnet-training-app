namespace DocumentTracker.Models;

public class InvoicePaymentSummary
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PreviouslyPaid { get; set; }
    public string? Notes { get; set; }
    public decimal RemainingBalance => TotalAmount - PreviouslyPaid;
}
