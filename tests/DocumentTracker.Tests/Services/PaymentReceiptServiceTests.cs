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
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ILogger<PaymentReceiptService>> _logger = new();

    [Fact]
    public async Task CreateAsync_WithValidReceipt_CreatesPaymentReceipt()
    {
        var service = CreateService();
        var viewModel = ValidCreateViewModel();

        _paymentReceiptRepository
            .Setup(repository => repository.GetNextReceiptSequenceAsync(It.IsAny<DateOnly>()))
            .ReturnsAsync(1);

        _productRepository
            .Setup(repository => repository.ListActiveAsync())
            .ReturnsAsync(ActiveProducts());

        _paymentReceiptRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>(), It.IsAny<IReadOnlyList<PaymentReceiptProduct>>()))
            .ReturnsAsync(11);

        var result = await service.CreateAsync(viewModel);

        Assert.True(result.Succeeded);
        Assert.Equal(11, result.Value);
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.Is<PaymentReceipt>(paymentReceipt =>
            paymentReceipt.ReceiptNumber.StartsWith("RCP-") &&
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
        Assert.Contains(result.Errors, error => error.Key == nameof(PaymentReceiptCreateViewModel.Products));
        _paymentReceiptRepository.Verify(repository => repository.CreateAsync(It.IsAny<PaymentReceipt>(), It.IsAny<IReadOnlyList<PaymentReceiptProduct>>()), Times.Never);
    }

    private PaymentReceiptService CreateService() => new(_paymentReceiptRepository.Object, _productRepository.Object, _logger.Object);

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
}
