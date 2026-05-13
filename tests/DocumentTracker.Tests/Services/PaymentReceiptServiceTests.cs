using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace DocumentTracker.Tests.Services;

public class PaymentReceiptServiceTests
{
    private readonly Mock<IPaymentReceiptRepository> _paymentReceiptRepository = new();
    private readonly Mock<IInvoiceLookupRepository> _invoiceLookupRepository = new();
    private readonly Mock<IPaymentReceiptNumberSequenceProvider> _sequenceProvider = new();
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
            .Setup(repository => repository.InvoiceExistsAndActiveAsync(1))
            .ReturnsAsync(true);

        _sequenceProvider
            .Setup(provider => provider.GetNextReceiptSequenceAsync())
            .ReturnsAsync(1026);

        _paymentReceiptRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()))
            .ReturnsAsync(11);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        Assert.Equal(11, result.Value);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.Is<PaymentReceipt>(paymentReceipt =>
            paymentReceipt.ReceiptNumber == "PR-1026" &&
            paymentReceipt.InvoiceId == 1 &&
            paymentReceipt.PaymentDate == new DateOnly(2026, 5, 13) &&
            paymentReceipt.AmountPaid == 500m &&
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
    public async Task CreateAsync_WithDraftInvoice_ReturnsValidationError()
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
                PreviouslyPaid = 0m
            });
        _paymentReceiptRepository
            .Setup(repository => repository.InvoiceExistsAndActiveAsync(1))
            .ReturnsAsync(false);

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == nameof(PaymentReceiptCreateViewModel.InvoiceNumber));
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenReceiptSequenceCollides_RetriesAndSucceeds()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _invoiceLookupRepository
            .Setup(repository => repository.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(OpenInvoiceSummary());
        _paymentReceiptRepository
            .Setup(repository => repository.InvoiceExistsAndActiveAsync(1))
            .ReturnsAsync(true);

        _sequenceProvider
            .SetupSequence(provider => provider.GetNextReceiptSequenceAsync())
            .ReturnsAsync(1026)
            .ReturnsAsync(1027);

        _paymentReceiptRepository
            .SetupSequence(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()))
            .ThrowsAsync(CreateUniqueViolation())
            .ReturnsAsync(11);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        Assert.Equal(11, result.Value);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>()), Times.Exactly(2));
    }

    private PaymentReceiptService CreateService() => new(
        _paymentReceiptRepository.Object,
        _invoiceLookupRepository.Object,
        _sequenceProvider.Object,
        _logger.Object);

    private static PaymentReceiptCreateViewModel ValidCreateViewModel() => new()
    {
        InvoiceNumber = "INV-1001",
        PaymentDate = new DateOnly(2026, 5, 13),
        AmountPaid = 500m,
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

    private static PostgresException CreateUniqueViolation() =>
        new(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation);
}
