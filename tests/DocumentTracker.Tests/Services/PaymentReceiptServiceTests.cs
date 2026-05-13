using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
using Moq;

namespace DocumentTracker.Tests.Services;

public class PaymentReceiptServiceTests
{
    private readonly Mock<IPaymentReceiptRepository> _paymentReceiptRepository = new();

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

    private PaymentReceiptService CreateService() => new(_paymentReceiptRepository.Object);
}
