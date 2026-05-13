using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
using Moq;

namespace DocumentTracker.Tests.Services;

public class PaymentReceiptCreatePageServiceTests
{
    [Fact]
    public async Task BuildAsync_WithInvoiceNumber_LoadsInvoiceSummary()
    {
        var repository = new Mock<IInvoiceLookupRepository>();
        repository
            .Setup(lookup => lookup.GetPaymentReceiptSummaryByNumberAsync("INV-1001"))
            .ReturnsAsync(new InvoicePaymentSummary
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
            });

        var service = new PaymentReceiptCreatePageService(repository.Object);

        var result = await service.BuildAsync("INV-1001");

        Assert.Equal("INV-1001", result.InvoiceNumber);
        Assert.Equal(1, result.InvoiceId);
        Assert.NotNull(result.InvoiceSummary);
        Assert.Equal(1000m, result.InvoiceSummary!.RemainingBalance);
    }
}
