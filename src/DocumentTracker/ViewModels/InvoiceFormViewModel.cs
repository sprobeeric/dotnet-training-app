using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;

namespace DocumentTracker.ViewModels;

public abstract class InvoiceFormViewModel : IValidatableObject
{
    [Display(Name = "Invoice Number")]
    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Display(Name = "Customer Name")]
    [Required]
    [StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Invoice Date")]
    [Required]
    public DateOnly InvoiceDate { get; set; }

    [Display(Name = "Due Date")]
    [Required]
    public DateOnly DueDate { get; set; }

    [Required]
    public InvoiceStatus? Status { get; set; }

    [Display(Name = "Subtotal")]
    [Required]
    [Range(0, 999999999)]
    public decimal Subtotal { get; set; }

    [Display(Name = "Tax Amount")]
    [Required]
    [Range(0, 999999999)]
    public decimal TaxAmount { get; set; }

    [Display(Name = "Total Amount")]
    [Required]
    [Range(0, 999999999)]
    public decimal TotalAmount { get; set; }

    [Display(Name = "Notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDate < InvoiceDate)
        {
            yield return new ValidationResult(
                "Due date must be on or after the invoice date.",
                [nameof(DueDate)]);
        }
    }
}
