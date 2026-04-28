using DocumentTracker.ViewModels;

namespace DocumentTracker.Services;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentListItemViewModel>> SearchAsync(string? searchTerm);
    Task<DocumentDetailsViewModel?> GetDetailsAsync(int id);
    Task<ServiceResult<int>> CreateAsync(DocumentCreateViewModel viewModel);
    Task<ServiceResult<DocumentEditViewModel>> GetEditAsync(int id);
    Task<ServiceResult> UpdateAsync(DocumentEditViewModel viewModel);
    Task<DocumentDeleteViewModel?> GetDeleteAsync(int id);
    Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole);
}
