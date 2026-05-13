using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Moq;

namespace DocumentTracker.Tests.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _repository = new();

    [Fact]
    public async Task SearchAsync_MapsInvoiceResults()
    {
        var invoiceDateFrom = new DateOnly(2026, 5, 1);
        var invoiceDateTo = new DateOnly(2026, 5, 31);
        var service = new InvoiceService(_repository.Object);

        _repository
            .Setup(repository => repository.SearchAsync("INV", invoiceDateFrom, invoiceDateTo, "total", "desc", 2, 10))
            .ReturnsAsync(new PagedResult<Invoice>
            {
                Items = [
                    new Invoice
                    {
                        Id = 11,
                        InvoiceNumber = "INV-001",
                        CustomerName = "Northwind",
                        InvoiceDate = new DateOnly(2026, 5, 1),
                        DueDate = new DateOnly(2026, 5, 31),
                        Status = InvoiceStatus.Sent,
                        TotalAmount = 250m,
                        UpdatedAtUtc = new DateTime(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc)
                    }
                ],
                TotalCount = 12
            });

        var result = await service.SearchAsync("INV", invoiceDateFrom, invoiceDateTo, "total", "desc", 2, 10);

        var invoice = Assert.Single(result.Items);
        Assert.Equal("INV-001", invoice.InvoiceNumber);
        Assert.Equal("Northwind", invoice.CustomerName);
        Assert.Equal(250m, invoice.TotalAmount);
        Assert.Equal(InvoiceStatus.Sent, invoice.Status);
        Assert.Equal(12, result.TotalCount);
    }
    [Fact]
    public async Task UpdateAsync_WithValidationFailure_DoesNotCallRepositoryUpdate()
    {
        var service = CreateService();
        var viewModel = ValidEditViewModel();
        viewModel.InvoiceNumber = string.Empty;

        var result = await service.UpdateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(InvoiceEditViewModel.InvoiceNumber));
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenInvoiceIsMissing_ReturnsNotFoundError()
    {
        var service = CreateService();
        var viewModel = ValidEditViewModel();

        _repository
            .Setup(repository => repository.GetByIdAsync(viewModel.Id))
            .ReturnsAsync((Invoice?)null);

        var result = await service.UpdateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenInvoiceIsDeleted_ReturnsDeletedInvoiceError()
    {
        var service = CreateService();
        var viewModel = ValidEditViewModel();

        _repository
            .Setup(repository => repository.GetByIdAsync(viewModel.Id))
            .ReturnsAsync(new Invoice
            {
                Id = viewModel.Id,
                InvoiceNumber = viewModel.InvoiceNumber,
                DeletedAtUtc = DateTime.UtcNow
            });

        var result = await service.UpdateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Message.Contains("Deleted documents cannot be edited", StringComparison.OrdinalIgnoreCase));
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateInvoiceNumber_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidEditViewModel();

        _repository
            .Setup(repository => repository.GetByIdAsync(viewModel.Id))
            .ReturnsAsync(new Invoice { Id = viewModel.Id, InvoiceNumber = "INV-100" });

        _repository
            .Setup(repository => repository.GetByInvoiceNumberAsync("INV-100"))
            .ReturnsAsync(new Invoice { Id = 7, InvoiceNumber = "INV-101" });

        var result = await service.UpdateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(InvoiceEditViewModel.InvoiceNumber));
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithValidInvoice_UpdatesInvoice()
    {
        var service = CreateService();
        var viewModel = ValidEditViewModel();

        _repository
            .Setup(repository => repository.GetByIdAsync(viewModel.Id))
            .ReturnsAsync(new Invoice
            {
                Id = viewModel.Id,
                InvoiceNumber = "OLD-100",
                CustomerName = "Old Customer",
                InvoiceDate = new DateOnly(2026, 1, 1),
                DueDate = new DateOnly(2026, 1, 15),
                Status = InvoiceStatus.Draft,
                Subtotal = 100m,
                TaxAmount = 12m,
                TotalAmount = 112m,
                Notes = "Old notes"
            });

        _repository
            .Setup(repository => repository.GetByInvoiceNumberAsync("INV-100"))
            .ReturnsAsync((Invoice?)null);

        _repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<Invoice>()))
            .ReturnsAsync(true);

        var result = await service.UpdateAsync(viewModel);

        Assert.True(result.Succeeded);
        _repository.Verify(repository => repository.UpdateAsync(It.Is<Invoice>(invoice =>
            invoice.Id == viewModel.Id &&
            invoice.InvoiceNumber == "INV-100" &&
            invoice.CustomerName == "Acme Corp" &&
            invoice.InvoiceDate == new DateOnly(2026, 5, 1) &&
            invoice.DueDate == new DateOnly(2026, 5, 31) &&
            invoice.Status == InvoiceStatus.Draft &&
            invoice.Subtotal == 1000m &&
            invoice.TaxAmount == 120m &&
            invoice.TotalAmount == 1120m &&
            invoice.Notes == "Updated invoice notes" &&
            invoice.UpdatedAtUtc.HasValue &&
            invoice.UpdatedAtUtc.Value.Kind == DateTimeKind.Utc)), Times.Once);
    }

    private InvoiceService CreateService() => new(_repository.Object);

    private static InvoiceEditViewModel ValidEditViewModel() => new()
    {
        Id = 5,
        InvoiceNumber = "INV-100",
        CustomerName = "Acme Corp",
        InvoiceDate = new DateOnly(2026, 5, 1),
        DueDate = new DateOnly(2026, 5, 31),
        Status = InvoiceStatus.Draft,
        Subtotal = 1000m,
        TaxAmount = 120m,
        TotalAmount = 1120m,
        Notes = "Updated invoice notes"
    };
}