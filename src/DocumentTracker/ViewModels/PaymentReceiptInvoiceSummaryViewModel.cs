using DocumentTracker.Models;

namespace DocumentTracker.ViewModels;

public class PaymentReceiptInvoiceSummaryViewModel
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal InvoiceTotal { get; set; }
    public decimal PreviouslyPaid { get; set; }
    public decimal RemainingBalance { get; set; }
    public string? Notes { get; set; }
    public bool AllowsPayment => Status == InvoiceStatus.Pending;
}
