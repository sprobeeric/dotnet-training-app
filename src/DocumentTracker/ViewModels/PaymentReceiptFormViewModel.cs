using System.ComponentModel.DataAnnotations;

namespace DocumentTracker.ViewModels;

public abstract class PaymentReceiptFormViewModel
{
    [Required]
    [Display(Name = "Receipt Number")]
    [StringLength(50)]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Invoice Number")]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int? InvoiceId { get; set; }

    [Required]
    [Display(Name = "Payment Date")]
    [DataType(DataType.Date)]
    public DateOnly? PaymentDate { get; set; }

    [Required]
    [Display(Name = "Amount Paid")]
    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal? AmountPaid { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    [StringLength(30)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Display(Name = "Reference Number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public PaymentReceiptInvoiceSummaryViewModel? InvoiceSummary { get; set; }

    public bool HasInvoiceSummary => InvoiceSummary is not null;
}
