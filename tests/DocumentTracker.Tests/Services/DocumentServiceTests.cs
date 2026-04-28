using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentTracker.Tests.Services;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _repository = new();
    private readonly Mock<ILogger<DocumentService>> _logger = new();

    [Fact]
    public async Task CreateAsync_WithValidDocument_CreatesDocument()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _repository
            .Setup(repository => repository.GetByDocumentNumberAsync("ENG-100"))
            .ReturnsAsync((Document?)null);

        _repository
            .Setup(repository => repository.CreateAsync(It.IsAny<Document>()))
            .ReturnsAsync(42);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        Assert.Equal(42, result.Value);
        _repository.Verify(repository => repository.CreateAsync(It.Is<Document>(document =>
            document.Title == "Runbook" &&
            document.DocumentNumber == "ENG-100" &&
            document.Status == DocumentStatus.Draft &&
            document.CreatedAtUtc.Kind == DateTimeKind.Utc &&
            document.UpdatedAtUtc.Kind == DateTimeKind.Utc)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidationFailure_DoesNotCallRepositoryCreate()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();
        viewModel.Title = string.Empty;

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(DocumentCreateViewModel.Title));
        _repository.Verify(repository => repository.CreateAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateDocumentNumber_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _repository
            .Setup(repository => repository.GetByDocumentNumberAsync("ENG-100"))
            .ReturnsAsync(new Document { Id = 7, DocumentNumber = "ENG-100" });

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(DocumentCreateViewModel.DocumentNumber));
        _repository.Verify(repository => repository.CreateAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDocumentIsMissing_ReturnsNotFoundError()
    {
        var service = CreateService();
        var viewModel = ValidEditViewModel();

        _repository
            .Setup(repository => repository.GetByIdAsync(viewModel.Id))
            .ReturnsAsync((Document?)null);

        var result = await service.UpdateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenDocumentIsDeleted_ReturnsDeletedDocumentError()
    {
        var service = CreateService();
        var viewModel = ValidEditViewModel();

        _repository
            .Setup(repository => repository.GetByIdAsync(viewModel.Id))
            .ReturnsAsync(new Document
            {
                Id = viewModel.Id,
                DocumentNumber = viewModel.DocumentNumber,
                DeletedAtUtc = DateTime.UtcNow
            });

        var result = await service.UpdateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message.Contains("Deleted documents cannot be edited", StringComparison.OrdinalIgnoreCase));
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Document>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteAsync_WithoutDocumentAdminRole_ReturnsRoleError()
    {
        var service = CreateService();

        var result = await service.SoftDeleteAsync(1, "Reviewer");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message.Contains("DocumentAdmin", StringComparison.Ordinal));
        _repository.Verify(repository => repository.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    private DocumentService CreateService() => new(_repository.Object, _logger.Object);

    private static DocumentCreateViewModel ValidCreateViewModel() => new()
    {
        Title = "Runbook",
        DocumentNumber = "ENG-100",
        Department = "Engineering",
        OwnerName = "Riley Cruz",
        Status = DocumentStatus.Draft,
        Description = "Support runbook."
    };

    private static DocumentEditViewModel ValidEditViewModel() => new()
    {
        Id = 5,
        Title = "Runbook",
        DocumentNumber = "ENG-100",
        Department = "Engineering",
        OwnerName = "Riley Cruz",
        Status = DocumentStatus.Review,
        Description = "Updated support runbook."
    };
}
