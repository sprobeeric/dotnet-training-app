using System.ComponentModel.DataAnnotations;

namespace DocumentTracker.ViewModels;

public class PaymentReceiptCreateViewModel
{
    [Display(Name = "Amount Received")]
    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal Received { get; set; }

    public List<PaymentReceiptProductInputViewModel> Products { get; set; } = [];
}
