using System.ComponentModel.DataAnnotations;

namespace DocumentTracker.ViewModels;

public class PaymentReceiptProductInputViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }

    [Range(0, 999)]
    public int Quantity { get; set; }
}
