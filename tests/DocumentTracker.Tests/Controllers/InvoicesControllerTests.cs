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
            .Setup(invoiceService => invoiceService.SearchAsync("ACME", invoiceDateFrom, invoiceDateTo, 1, 5))
            .ReturnsAsync(new PagedResult<InvoiceListItemViewModel>
            {
                Items = [
                    new InvoiceListItemViewModel
                    {
                        Id = 5,
                        InvoiceNumber = "INV-100",
                        CustomerName = "ACME",
                        Status = InvoiceStatus.Sent,
                        TotalAmount = 125.50m
                    }
                ],
                TotalCount = 1
            });

        var controller = new InvoicesController(service.Object);

        var result = await controller.Index("ACME", invoiceDateFrom, invoiceDateTo);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<InvoiceSearchViewModel>(viewResult.Model);
        Assert.Equal("ACME", model.SearchTerm);
        Assert.Equal(invoiceDateFrom, model.InvoiceDateFrom);
        Assert.Equal(invoiceDateTo, model.InvoiceDateTo);
        Assert.Equal(1, model.PageNumber);
        Assert.Equal(5, model.PageSize);
        Assert.Equal(1, model.TotalCount);
        Assert.Single(model.Invoices);
    }

    [Fact]
    public async Task Index_WhenPageIsLessThanOne_UsesFirstPage()
    {
        var service = new Mock<IInvoiceService>();
        service
            .Setup(invoiceService => invoiceService.SearchAsync(null, null, null, 1, 5))
            .ReturnsAsync(new PagedResult<InvoiceListItemViewModel>());

        var controller = new InvoicesController(service.Object);

        await controller.Index(null, null, null, 0);

        service.Verify(invoiceService => invoiceService.SearchAsync(null, null, null, 1, 5), Times.Once);
    }

    [Fact]
    public async Task Index_WhenPageIsBeyondLastPage_LoadsLastAvailablePage()
    {
        var service = new Mock<IInvoiceService>();
        service
            .SetupSequence(invoiceService => invoiceService.SearchAsync("ACME", null, null, 9, 5))
            .ReturnsAsync(new PagedResult<InvoiceListItemViewModel>
            {
                TotalCount = 21
            });
        service
            .Setup(invoiceService => invoiceService.SearchAsync("ACME", null, null, 5, 5))
            .ReturnsAsync(new PagedResult<InvoiceListItemViewModel>
            {
                Items = [
                    new InvoiceListItemViewModel
                    {
                        Id = 8,
                        InvoiceNumber = "INV-008"
                    }
                ],
                TotalCount = 21
            });

        var controller = new InvoicesController(service.Object);

        var result = await controller.Index("ACME", null, null, 9);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<InvoiceSearchViewModel>(viewResult.Model);
        Assert.Equal(5, model.PageNumber);
        Assert.Single(model.Invoices);
        service.Verify(invoiceService => invoiceService.SearchAsync("ACME", null, null, 9, 5), Times.Once);
        service.Verify(invoiceService => invoiceService.SearchAsync("ACME", null, null, 5, 5), Times.Once);
    }
}