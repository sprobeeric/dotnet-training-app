using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;

namespace DocumentTracker.ViewModels;

public class PaymentReceiptCreateViewModel
{
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
}
