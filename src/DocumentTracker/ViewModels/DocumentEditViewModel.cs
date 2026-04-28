using System.ComponentModel.DataAnnotations;

namespace DocumentTracker.ViewModels;

public class DocumentEditViewModel : DocumentFormViewModel
{
    [Required]
    public int Id { get; set; }
}
