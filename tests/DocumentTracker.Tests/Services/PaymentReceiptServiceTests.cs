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
    public async Task CreateAsync_WithValidReceipt_CreatesPaymentReceipt()
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
            .Setup(repository => repository.ReceiptNumberExistsAsync("RCT-0001", null))
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
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.Is<PaymentReceipt>(paymentReceipt =>
            paymentReceipt.ReceiptNumber == "RCT-0001" &&
            paymentReceipt.InvoiceId == 1 &&
            paymentReceipt.PaymentDate == new DateOnly(2026, 5, 13) &&
            paymentReceipt.AmountPaid == 1000m &&
            paymentReceipt.PaymentMethod == "Bank Transfer" &&
            paymentReceipt.ReferenceNumber == "REF-1001" &&
            paymentReceipt.Notes == "Partial payment received." &&
            paymentReceipt.CreatedAtUtc.HasValue &&
            paymentReceipt.UpdatedAtUtc.HasValue)), Times.Once);
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
            .Setup(repository => repository.InvoiceExistsAndPendingAsync(1))
            .ReturnsAsync(true);
        _paymentReceiptRepository
            .Setup(repository => repository.ReceiptNumberExistsAsync("RCT-0001", null))
            .ReturnsAsync(false);
        _paymentReceiptRepository
            .Setup(repository => repository.ReferenceNumberExistsAsync("REF-1001", null))
            .ReturnsAsync(false);
        _paymentReceiptRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()))
            .ReturnsAsync(11);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.Is<PaymentReceipt>(paymentReceipt =>
            paymentReceipt.AmountPaid == 250m)), Times.Once);
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
            .Setup(repository => repository.ReceiptNumberExistsAsync("RCT-0001", null))
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
            .Setup(repository => repository.ReceiptNumberExistsAsync("RCT-0001", null))
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
            .Setup(repository => repository.ReceiptNumberExistsAsync("RCT-0001", null))
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
        ReceiptNumber = "RCT-0001",
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
