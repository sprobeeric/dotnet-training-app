namespace DocumentTracker.ViewModels;

public class DocumentSearchViewModel
{
    public string? SearchTerm { get; set; }
    public IReadOnlyList<DocumentListItemViewModel> Documents { get; set; } = [];
}
