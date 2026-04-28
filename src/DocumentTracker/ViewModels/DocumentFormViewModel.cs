using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;

namespace DocumentTracker.ViewModels;

public abstract class DocumentFormViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Document Number")]
    [Required]
    [StringLength(50)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "Owner Name")]
    [Required]
    [StringLength(120)]
    public string OwnerName { get; set; } = string.Empty;

    [Required]
    public DocumentStatus? Status { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}
