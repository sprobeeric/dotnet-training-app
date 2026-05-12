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
    private readonly Mock<IPaymentReceiptNumberGenerator> _numberGenerator = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly PaymentReceiptValidator _validator = new();
    private readonly Mock<ILogger<PaymentReceiptService>> _logger = new();

    [Fact]
    public async Task CreateAsync_WithValidReceipt_CreatesPaymentReceipt()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _productRepository
            .Setup(repository => repository.ListActiveAsync())
            .ReturnsAsync(ActiveProducts());

        _numberGenerator
            .Setup(generator => generator.GenerateAsync())
            .ReturnsAsync(("PR-20260512-000001", "REF-20260512-000001-ABC123", DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 0, 0), DateTimeKind.Utc)));

        _paymentReceiptRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>(), It.IsAny<IReadOnlyList<PaymentReceiptProduct>>()))
            .ReturnsAsync(11);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        Assert.Equal(11, result.Value);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.Is<PaymentReceipt>(paymentReceipt =>
            paymentReceipt.ReceiptNumber.StartsWith("PR-") &&
            paymentReceipt.ReferenceNumber.StartsWith("REF-") &&
            paymentReceipt.TotalAmount == 350m &&
            paymentReceipt.ChangeAmount == 50m &&
            paymentReceipt.CreatedAtUtc.Kind == DateTimeKind.Utc &&
            paymentReceipt.UpdatedAtUtc.Kind == DateTimeKind.Utc),
            It.Is<IReadOnlyList<PaymentReceiptProduct>>(products =>
                products.Count == 2 &&
                products.Any(product => product.ProductId == 1 && product.Quantity == 2 && product.LineTotal == 240m) &&
                products.Any(product => product.ProductId == 2 && product.Quantity == 1 && product.LineTotal == 110m))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNoSelectedProducts_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();
        viewModel.Products.ForEach(product => product.Quantity = 0);

        _productRepository
            .Setup(repository => repository.ListActiveAsync())
            .ReturnsAsync(ActiveProducts());

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == string.Empty);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>(), It.IsAny<IReadOnlyList<PaymentReceiptProduct>>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithQuantityBelowZero_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();
        viewModel.Products[0].Quantity = -1;

        _productRepository
            .Setup(repository => repository.ListActiveAsync())
            .ReturnsAsync(ActiveProducts());

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == "Products[0].Quantity");
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>(), It.IsAny<IReadOnlyList<PaymentReceiptProduct>>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithQuantityAboveMaximum_ReturnsValidationError()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();
        viewModel.Products[0].Quantity = 1000;

        _productRepository
            .Setup(repository => repository.ListActiveAsync())
            .ReturnsAsync(ActiveProducts());

        var result = await service.CreateAsync(viewModel);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Key == "Products[0].Quantity");
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>(), It.IsAny<IReadOnlyList<PaymentReceiptProduct>>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenReceiptSequenceCollides_RetriesAndSucceeds()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _productRepository
            .Setup(repository => repository.ListActiveAsync())
            .ReturnsAsync(ActiveProducts());

        _numberGenerator
            .SetupSequence(generator => generator.GenerateAsync())
            .ReturnsAsync(("PR-20260512-000001", "REF-20260512-000001-ABC123", DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 0, 0), DateTimeKind.Utc)))
            .ReturnsAsync(("PR-20260512-000002", "REF-20260512-000002-XYZ789", DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 0, 1), DateTimeKind.Utc)));

        _paymentReceiptRepository
            .SetupSequence(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>(), It.IsAny<IReadOnlyList<PaymentReceiptProduct>>()))
            .ThrowsAsync(CreateUniqueViolation())
            .ReturnsAsync(11);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        Assert.Equal(11, result.Value);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>(), It.IsAny<IReadOnlyList<PaymentReceiptProduct>>()), Times.Exactly(2));
    }

    private PaymentReceiptService CreateService() => new(
        _paymentReceiptRepository.Object,
        _numberGenerator.Object,
        _productRepository.Object,
        _validator,
        _logger.Object);

    private static PaymentReceiptCreateViewModel ValidCreateViewModel() => new()
    {
        Received = 400m,
        Products =
        [
            new PaymentReceiptProductInputViewModel { ProductId = 1, Quantity = 2 },
            new PaymentReceiptProductInputViewModel { ProductId = 2, Quantity = 1 }
        ]
    };

    private static IReadOnlyList<Product> ActiveProducts() =>
    [
        new Product { Id = 1, Name = "Classic Pearl Milk Tea", UnitPrice = 120m },
        new Product { Id = 2, Name = "Wintermelon Milk Tea", UnitPrice = 110m }
    ];

    private static PostgresException CreateUniqueViolation() =>
        new(
            messageText: "duplicate key value violates unique constraint",
            severity: "ERROR",
            invariantSeverity: "ERROR",
            sqlState: PostgresErrorCodes.UniqueViolation);
}
