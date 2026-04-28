using DocumentTracker.Models;

namespace DocumentTracker.ViewModels;

public class DocumentDeleteViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
}
