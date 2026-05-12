using DocumentTracker.Models;
using DocumentTracker.Repositories;
using DocumentTracker.Services;
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
            .Setup(repository => repository.SearchAsync("INV", invoiceDateFrom, invoiceDateTo, 2, 10))
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

        var result = await service.SearchAsync("INV", invoiceDateFrom, invoiceDateTo, 2, 10);

        var invoice = Assert.Single(result.Items);
        Assert.Equal("INV-001", invoice.InvoiceNumber);
        Assert.Equal("Northwind", invoice.CustomerName);
        Assert.Equal(250m, invoice.TotalAmount);
        Assert.Equal(InvoiceStatus.Sent, invoice.Status);
        Assert.Equal(12, result.TotalCount);
    }
}