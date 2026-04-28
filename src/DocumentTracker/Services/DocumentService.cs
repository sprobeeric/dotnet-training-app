using System.ComponentModel.DataAnnotations;
using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.ViewModels;
using Npgsql;

namespace DocumentTracker.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(IDocumentRepository repository, ILogger<DocumentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentListItemViewModel>> SearchAsync(string? searchTerm)
    {
        var documents = await _repository.SearchAsync(searchTerm);
        return documents.Select(ToListItem).ToList();
    }

    public async Task<DocumentDetailsViewModel?> GetDetailsAsync(int id)
    {
        var document = await _repository.GetByIdAsync(id);
        return document is null || document.DeletedAtUtc is not null ? null : ToDetails(document);
    }

    public async Task<ServiceResult<int>> CreateAsync(DocumentCreateViewModel viewModel)
    {
        var validationResult = Validate(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var existing = await _repository.GetByDocumentNumberAsync(viewModel.DocumentNumber);
        if (existing is not null)
        {
            return ServiceResult<int>.Failure(nameof(viewModel.DocumentNumber), "A document with this document number already exists.");
        }

        var now = DateTime.UtcNow;
        var document = new Document
        {
            Title = viewModel.Title.Trim(),
            DocumentNumber = viewModel.DocumentNumber.Trim(),
            Department = viewModel.Department.Trim(),
            OwnerName = viewModel.OwnerName.Trim(),
            Status = viewModel.Status!.Value,
            Description = string.IsNullOrWhiteSpace(viewModel.Description) ? null : viewModel.Description.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        try
        {
            var id = await _repository.CreateAsync(document);
            return ServiceResult<int>.Success(id);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            _logger.LogWarning(ex, "Document create failed because the document number was not unique.");
            return ServiceResult<int>.Failure(nameof(viewModel.DocumentNumber), "A document with this document number already exists.");
        }
    }

    public async Task<ServiceResult<DocumentEditViewModel>> GetEditAsync(int id)
    {
        var document = await _repository.GetByIdAsync(id);
        if (document is null)
        {
            return ServiceResult<DocumentEditViewModel>.Failure(string.Empty, "The document was not found.");
        }

        if (document.DeletedAtUtc is not null)
        {
            return ServiceResult<DocumentEditViewModel>.Failure(string.Empty, "Deleted documents cannot be edited.");
        }

        return ServiceResult<DocumentEditViewModel>.Success(ToEdit(document));
    }

    public async Task<ServiceResult> UpdateAsync(DocumentEditViewModel viewModel)
    {
        var validationResult = Validate(viewModel);
        if (!validationResult.Succeeded)
        {
            return validationResult;
        }

        var existing = await _repository.GetByIdAsync(viewModel.Id);
        if (existing is null)
        {
            return ServiceResult.Failure(string.Empty, "The document was not found.");
        }

        if (existing.DeletedAtUtc is not null)
        {
            return ServiceResult.Failure(string.Empty, "Deleted documents cannot be edited.");
        }

        var duplicate = await _repository.GetByDocumentNumberAsync(viewModel.DocumentNumber);
        if (duplicate is not null && duplicate.Id != viewModel.Id)
        {
            return ServiceResult.Failure(nameof(viewModel.DocumentNumber), "A document with this document number already exists.");
        }

        existing.Title = viewModel.Title.Trim();
        existing.DocumentNumber = viewModel.DocumentNumber.Trim();
        existing.Department = viewModel.Department.Trim();
        existing.OwnerName = viewModel.OwnerName.Trim();
        existing.Status = viewModel.Status!.Value;
        existing.Description = string.IsNullOrWhiteSpace(viewModel.Description) ? null : viewModel.Description.Trim();
        existing.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        return updated
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Empty, "The document could not be updated. It may have been deleted by another user.");
    }

    public async Task<DocumentDeleteViewModel?> GetDeleteAsync(int id)
    {
        var document = await _repository.GetByIdAsync(id);
        return document is null || document.DeletedAtUtc is not null ? null : ToDelete(document);
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id, string? currentRole)
    {
        if (!string.Equals(currentRole, DocumentRoles.DocumentAdmin, StringComparison.Ordinal))
        {
            return ServiceResult.Failure(string.Empty, "Only users in the DocumentAdmin role can delete documents.");
        }

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult.Failure(string.Empty, "The document was not found.");
        }

        if (existing.DeletedAtUtc is not null)
        {
            return ServiceResult.Failure(string.Empty, "This document has already been deleted.");
        }

        var deleted = await _repository.SoftDeleteAsync(id, DateTime.UtcNow);
        return deleted
            ? ServiceResult.Success()
            : ServiceResult.Failure(string.Empty, "The document could not be deleted. It may have been changed by another user.");
    }

    private static ServiceResult<int> Validate(DocumentCreateViewModel viewModel)
    {
        var result = new ServiceResult<int>();
        AddValidationErrors(viewModel, result);
        return result;
    }

    private static ServiceResult Validate(DocumentEditViewModel viewModel)
    {
        var result = new ServiceResult();
        AddValidationErrors(viewModel, result);
        return result;
    }

    private static void AddValidationErrors(object viewModel, ServiceResult result)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(viewModel);

        Validator.TryValidateObject(viewModel, context, validationResults, validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var key = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
            result.AddError(key, validationResult.ErrorMessage ?? "The value is invalid.");
        }
    }

    private static DocumentListItemViewModel ToListItem(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        DocumentNumber = document.DocumentNumber,
        Department = document.Department,
        OwnerName = document.OwnerName,
        Status = document.Status,
        UpdatedAtUtc = document.UpdatedAtUtc
    };

    private static DocumentDetailsViewModel ToDetails(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        DocumentNumber = document.DocumentNumber,
        Department = document.Department,
        OwnerName = document.OwnerName,
        Status = document.Status,
        Description = document.Description,
        CreatedAtUtc = document.CreatedAtUtc,
        UpdatedAtUtc = document.UpdatedAtUtc,
        DeletedAtUtc = document.DeletedAtUtc
    };

    private static DocumentEditViewModel ToEdit(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        DocumentNumber = document.DocumentNumber,
        Department = document.Department,
        OwnerName = document.OwnerName,
        Status = document.Status,
        Description = document.Description
    };

    private static DocumentDeleteViewModel ToDelete(Document document) => new()
    {
        Id = document.Id,
        Title = document.Title,
        DocumentNumber = document.DocumentNumber,
        Department = document.Department,
        OwnerName = document.OwnerName,
        Status = document.Status
    };
}
