using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentTracker.Tests.Services;

public class PaymentReceiptServiceTests
{
    private readonly Mock<IPaymentReceiptRepository> _paymentReceiptRepository = new();
    private readonly Mock<IInvoiceLookupRepository> _invoiceLookupRepository = new();
    private readonly Mock<ILogger<PaymentReceiptService>> _logger = new();

    [Fact]
    public async Task GetDetailsAsync_WhenReceiptExists_ReturnsInvoiceAndPaymentContext()
    {
        var service = CreateService();

        _paymentReceiptRepository
            .Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync(new PaymentReceipt
            {
                Id = 5,
                ReceiptNumber = "PR-1001",
                InvoiceId = 7,
                InvoiceNumber = "INV-1001",
                CustomerName = "Northwind Traders",
                PaymentDate = new DateOnly(2026, 5, 13),
                AmountPaid = 500m,
                PaymentMethod = "BankTransfer",
                ReferenceNumber = "REF-1001",
                Notes = "Partial payment received.",
                CreatedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 5, 13, 1, 0, 0), DateTimeKind.Utc)
            });
        var viewModel = ValidCreateViewModel();

        _invoiceLookupRepository
            .Setup(repository => repository.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(OpenInvoiceSummary());
        _paymentReceiptRepository
            .Setup(repository => repository.InvoiceExistsAndPendingAsync(1))
            .ReturnsAsync(true);
        _paymentReceiptRepository
            .Setup(repository => repository.ReceiptNumberExistsAsync("PR-0001", null))
            .ReturnsAsync(false);
        _paymentReceiptRepository
            .Setup(repository => repository.ReferenceNumberExistsAsync("REF-1001", null))
            .ReturnsAsync(false);

        _paymentReceiptRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()))
            .ReturnsAsync(11);

        var result = await service.GetDetailsAsync(5);

        Assert.NotNull(result);
        Assert.Equal("PR-1001", result.ReceiptNumber);
        Assert.Equal("INV-1001", result.InvoiceNumber);
        Assert.Equal("Northwind Traders", result.CustomerName);
        Assert.Equal(new DateOnly(2026, 5, 13), result.PaymentDate);
        Assert.Equal(500m, result.AmountPaid);
        Assert.Equal("BankTransfer", result.PaymentMethod);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenReceiptIsMissing_ReturnsNull()
    {
        var service = CreateService();

        _paymentReceiptRepository
            .Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync((PaymentReceipt?)null);

        var result = await service.GetDetailsAsync(5);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithMissingInvoice_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(PaymentReceiptCreateViewModel.InvoiceNumber));
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithAmountAboveRemainingBalance_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();
        viewModel.AmountPaid = 2000m;

        _invoiceLookupRepository
            .Setup(repository => repository.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(OpenInvoiceSummary());
        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(PaymentReceiptCreateViewModel.AmountPaid));
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithPartialAmountBelowRemainingBalance_CreatesPaymentReceipt()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();
        viewModel.AmountPaid = 250m;

        _invoiceLookupRepository
            .Setup(repository => repository.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(OpenInvoiceSummary());
        _paymentReceiptRepository
            .Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync((PaymentReceipt?)null);
        _paymentReceiptRepository
            .Setup(repository => repository.InvoiceExistsAndPendingAsync(1))
            .ReturnsAsync(true);
        _paymentReceiptRepository
            .Setup(repository => repository.ReceiptNumberExistsAsync("PR-0001", null))
            .ReturnsAsync(false);
        _paymentReceiptRepository
            .Setup(repository => repository.ReferenceNumberExistsAsync("REF-1001", null))
            .ReturnsAsync(false);
        _paymentReceiptRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()))
            .ReturnsAsync(11);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        Assert.Equal(11, result.Value);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.Is<PaymentReceipt>(receipt =>
            receipt.ReceiptNumber == "PR-0001" &&
            receipt.InvoiceId == 1 &&
            receipt.PaymentDate == new DateOnly(2026, 5, 13) &&
            receipt.AmountPaid == 250m &&
            receipt.PaymentMethod == "Bank Transfer" &&
            receipt.ReferenceNumber == "REF-1001" &&
            receipt.Notes == "Partial payment received.")), Times.Once);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenReceiptIsDeleted_ReturnsNull()
    {
        var service = CreateService();

        _paymentReceiptRepository
            .Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync(new PaymentReceipt
            {
                Id = 5,
                ReceiptNumber = "PR-1001",
                DeletedAtUtc = DateTime.UtcNow
            });

        var result = await service.GetDetailsAsync(5);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithNonPendingInvoice_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _invoiceLookupRepository
            .Setup(repository => repository.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(new InvoicePaymentSummary
            {
                InvoiceId = 1,
                InvoiceNumber = "INV-1001",
                CustomerName = "Northwind Traders",
                InvoiceDate = new DateOnly(2026, 5, 1),
                DueDate = new DateOnly(2026, 5, 30),
                Status = InvoiceStatus.Draft,
                TotalAmount = 1375m,
            });
        _paymentReceiptRepository
            .Setup(repository => repository.InvoiceExistsAndPendingAsync(1))
            .ReturnsAsync(false);

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(PaymentReceiptCreateViewModel.InvoiceNumber));
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateReceiptNumber_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _invoiceLookupRepository
            .Setup(repository => repository.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(OpenInvoiceSummary());
        _paymentReceiptRepository
            .Setup(repository => repository.InvoiceExistsAndPendingAsync(1))
            .ReturnsAsync(true);
        _paymentReceiptRepository
            .Setup(repository => repository.ReceiptNumberExistsAsync("PR-0001", null))
            .ReturnsAsync(true);

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(PaymentReceiptCreateViewModel.ReceiptNumber));
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateReferenceNumber_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _invoiceLookupRepository
            .Setup(repository => repository.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(OpenInvoiceSummary());
        _paymentReceiptRepository
            .Setup(repository => repository.InvoiceExistsAndPendingAsync(1))
            .ReturnsAsync(true);
        _paymentReceiptRepository
            .Setup(repository => repository.ReceiptNumberExistsAsync("PR-0001", null))
            .ReturnsAsync(false);
        _paymentReceiptRepository
            .Setup(repository => repository.ReferenceNumberExistsAsync("REF-1001", null))
            .ReturnsAsync(true);

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(PaymentReceiptCreateViewModel.ReferenceNumber));
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithCashPayment_ClearsReferenceNumber()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();
        viewModel.PaymentMethod = "Cash";
        viewModel.ReferenceNumber = "SHOULD-BE-CLEARED";

        _invoiceLookupRepository
            .Setup(repository => repository.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(OpenInvoiceSummary());
        _paymentReceiptRepository
            .Setup(repository => repository.InvoiceExistsAndPendingAsync(1))
            .ReturnsAsync(true);
        _paymentReceiptRepository
            .Setup(repository => repository.ReceiptNumberExistsAsync("PR-0001", null))
            .ReturnsAsync(false);

        _paymentReceiptRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()))
            .ReturnsAsync(11);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        _paymentReceiptRepository.Verify(repository => repository.ReferenceNumberExistsAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.Is<PaymentReceipt>(paymentReceipt =>
            paymentReceipt.ReferenceNumber == null &&
            paymentReceipt.PaymentMethod == "Cash")), Times.Once);
    }

    private PaymentReceiptService CreateService() => new(
        _paymentReceiptRepository.Object,
        _invoiceLookupRepository.Object,
        _logger.Object);

    private static PaymentReceiptCreateViewModel ValidCreateViewModel() => new()
    {
        ReceiptNumber = "PR-0001",
        InvoiceNumber = "INV-1001",
        PaymentDate = new DateOnly(2026, 5, 13),
        AmountPaid = 1000m,
        PaymentMethod = "Bank Transfer",
        ReferenceNumber = "REF-1001",
        Notes = "Partial payment received."
    };

    private static InvoicePaymentSummary OpenInvoiceSummary() => new()
    {
        InvoiceId = 1,
        InvoiceNumber = "INV-1001",
        CustomerName = "Northwind Traders",
        InvoiceDate = new DateOnly(2026, 5, 1),
        DueDate = new DateOnly(2026, 5, 30),
        Status = InvoiceStatus.Pending,
        TotalAmount = 1375m,
        PreviouslyPaid = 375m,
        Notes = "Initial consulting invoice."
    };

}
