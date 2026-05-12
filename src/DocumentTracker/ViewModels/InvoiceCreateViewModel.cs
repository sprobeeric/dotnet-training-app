using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;

namespace DocumentTracker.ViewModels;

// did not apply inherit since main form view model does not exist yet
public class InvoiceCreateViewModel : IValidatableObject
{
    [Display(Name = "Invoice Number")]
    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Display(Name = "Customer Name")]
    [Required]
    [StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Invoice Date")]
    [Required]
    public DateOnly InvoiceDate { get; set; }

    [Display(Name = "Due Date")]
    [Required]
    public DateOnly DueDate { get; set; }

    [Display(Name = "Status")]
    [Required]
    public InvoiceStatus? Status { get; set; }

    [Display(Name = "Subtotal")]
    [Range(0.01, 999999999.99)]
    public decimal Subtotal { get; set;}

    [Display(Name = "TaxAmount")]
    [Range(0.01, 999999999.99)]
    public decimal TaxAmount { get; set;}

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
