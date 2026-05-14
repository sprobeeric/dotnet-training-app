using System.ComponentModel.DataAnnotations;

namespace DocumentTracker.ViewModels;

public class PaymentReceiptEditViewModel : PaymentReceiptFormViewModel
{
    [Required]
    public int Id { get; set; }

    public int? ExistingInvoiceId { get; set; }
}
