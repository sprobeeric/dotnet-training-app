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
