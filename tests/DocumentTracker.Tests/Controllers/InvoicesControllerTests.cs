using DocumentTracker.Controllers;
using DocumentTracker.Models;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class InvoicesControllerTests
{
    [Fact]
    public async Task Index_ReturnsSearchViewModel()
    {
        var invoiceDateFrom = new DateOnly(2026, 5, 1);
        var invoiceDateTo = new DateOnly(2026, 5, 31);
        var service = new Mock<IInvoiceService>();
        service
            .Setup(invoiceService => invoiceService.SearchAsync("ACME", invoiceDateFrom, invoiceDateTo))
            .ReturnsAsync([
                new InvoiceListItemViewModel
                {
                    Id = 5,
                    InvoiceNumber = "INV-100",
                    CustomerName = "ACME",
                    Status = InvoiceStatus.Sent,
                    TotalAmount = 125.50m
                }
            ]);

        var controller = new InvoicesController(service.Object);

        var result = await controller.Index("ACME", invoiceDateFrom, invoiceDateTo);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<InvoiceSearchViewModel>(viewResult.Model);
        Assert.Equal("ACME", model.SearchTerm);
        Assert.Equal(invoiceDateFrom, model.InvoiceDateFrom);
        Assert.Equal(invoiceDateTo, model.InvoiceDateTo);
        Assert.Single(model.Invoices);
    }
}