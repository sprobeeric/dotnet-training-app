using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Moq;
using Microsoft.Extensions.Logging;

namespace DocumentTracker.Tests.Services;

public class InvoiceServiceTests
{
    private readonly Mock<IInvoiceRepository> _repository = new();

    [Fact]
    public async Task GetDeleteAsync_WhenInvoiceNotFound_ReturnsNull()
    {
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Invoice?)null);

        var result = await service.GetDeleteAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDeleteAsync_WhenInvoiceAlreadyDeleted_ReturnsNull()
    {
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Invoice
        {
            Id = 1,
            InvoiceNumber = "INV-001",
            DeletedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var result = await service.GetDeleteAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDeleteAsync_WhenInvoiceExists_ReturnsMappedViewModel()
    {
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);
        _repository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Invoice
        {
            Id = 5,
            InvoiceNumber = "INV-005",
            CustomerName = "Contoso",
            InvoiceDate = new DateOnly(2026, 5, 1),
            DueDate = new DateOnly(2026, 5, 31),
            Status = InvoiceStatus.Sent,
            TotalAmount = 500m
        });

        var result = await service.GetDeleteAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("INV-005", result.InvoiceNumber);
        Assert.Equal("Contoso", result.CustomerName);
        Assert.Equal(500m, result.TotalAmount);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenRoleIsNotInvoiceAdmin_ReturnsFailure()
    {
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);
        var result = await service.SoftDeleteAsync(1, null);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message == "Only users in the InvoiceAdmin role can delete invoices.");
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenInvoiceNotFound_ReturnsFailure()
    {
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Invoice?)null);

        var result = await service.SoftDeleteAsync(99, InvoiceRoles.InvoiceAdmin);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message == "The invoice was not found.");
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenInvoiceAlreadyDeleted_ReturnsFailure()
    {
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Invoice
        {
            Id = 1,
            DeletedAtUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var result = await service.SoftDeleteAsync(1, InvoiceRoles.InvoiceAdmin);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message == "This invoice has already been deleted.");
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenRepositoryReturnsFalse_ReturnsFailure()
    {
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);
        _repository.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Invoice { Id = 2 });
        _repository.Setup(r => r.SoftDeleteAsync(2, It.IsAny<DateTime>())).ReturnsAsync(false);

        var result = await service.SoftDeleteAsync(2, InvoiceRoles.InvoiceAdmin);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("could not be deleted"));
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenSuccessful_ReturnsSuccess()
    {
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);
        _repository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Invoice { Id = 3 });
        _repository.Setup(r => r.SoftDeleteAsync(3, It.IsAny<DateTime>())).ReturnsAsync(true);

        var result = await service.SoftDeleteAsync(3, InvoiceRoles.InvoiceAdmin);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SearchAsync_MapsInvoiceResults()
    {
        var invoiceDateFrom = new DateOnly(2026, 5, 1);
        var invoiceDateTo = new DateOnly(2026, 5, 31);
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(_repository.Object, logger.Object);

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

    private InvoiceService CreateService() => new(_repository.Object, Mock.Of<ILogger<InvoiceService>>());

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

    [Fact]
    public async Task CreateAsync_WithValidInvoice_CreatesInvoice()
    {
        var repository = new Mock<IInvoiceRepository>();
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(repository.Object, logger.Object);

        var viewModel = new InvoiceCreateViewModel
        {
            InvoiceNumber = "INV-100",
            CustomerName = "ACME",
            InvoiceDate = new DateOnly(2026, 5, 1),
            DueDate = new DateOnly(2026, 5, 15),
            Status = InvoiceStatus.Sent,
            Subtotal = 100m,
            TaxAmount = 12m
        };

        repository
            .Setup(repository => repository.InvoiceNumberExistsAsync("INV-100", null))
            .ReturnsAsync(false);

        repository
            .Setup(repository => repository.CreateAsync(It.IsAny<Invoice>()))
            .ReturnsAsync(10);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        Assert.Equal(10, result.Value);

        repository.Verify(
            repository => repository.CreateAsync(It.Is<Invoice>(invoice =>
                invoice.InvoiceNumber == "INV-100" &&
                invoice.CustomerName == "ACME" &&
                invoice.InvoiceDate == new DateOnly(2026, 5, 1) &&
                invoice.DueDate == new DateOnly(2026, 5, 15) &&
                invoice.Status == InvoiceStatus.Sent &&
                invoice.Subtotal == 100m &&
                invoice.TaxAmount == 12m &&
                invoice.TotalAmount == 112m)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateInvoiceNumber_ReturnsValidationError()
    {
        var repository = new Mock<IInvoiceRepository>();
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(repository.Object, logger.Object);

        var viewModel = new InvoiceCreateViewModel
        {
            InvoiceNumber = "INV-100",
            CustomerName = "ACME",
            InvoiceDate = new DateOnly(2026, 5, 1),
            DueDate = new DateOnly(2026, 5, 15),
            Status = InvoiceStatus.Sent,
            Subtotal = 100m,
            TaxAmount = 12m
        };

        repository
            .Setup(repository => repository.InvoiceNumberExistsAsync("INV-100", null))
            .ReturnsAsync(true);

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(InvoiceCreateViewModel.InvoiceNumber));
        repository.Verify(repository => repository.CreateAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDueDateIsBeforeInvoiceDate_ReturnsValidationError()
    {
        var repository = new Mock<IInvoiceRepository>();
        var logger = new Mock<ILogger<InvoiceService>>();
        var service = new InvoiceService(repository.Object, logger.Object);
    
        var viewModel = new InvoiceCreateViewModel
        {
            InvoiceNumber = "INV-100",
            CustomerName = "ACME",
            InvoiceDate = new DateOnly(2026, 5, 15),
            DueDate = new DateOnly(2026, 5, 1),
            Status = InvoiceStatus.Sent,
            Subtotal = 100m,
            TaxAmount = 12m
        };
    
        var result = await service.CreateAsync(viewModel);
    
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(InvoiceCreateViewModel.DueDate));
        repository.Verify(repository => repository.InvoiceNumberExistsAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        repository.Verify(repository => repository.CreateAsync(It.IsAny<Invoice>()), Times.Never);
    }
}
