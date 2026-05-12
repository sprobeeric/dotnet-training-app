using DocumentTracker.Models;

namespace DocumentTracker.ViewModels;

public class InvoiceListItemViewModel
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}