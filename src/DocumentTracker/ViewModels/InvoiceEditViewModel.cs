using System.ComponentModel.DataAnnotations;

namespace DocumentTracker.ViewModels;

public class InvoiceEditViewModel : InvoiceFormViewModel
{
    [Required]
    public int Id { get; set; }
}
