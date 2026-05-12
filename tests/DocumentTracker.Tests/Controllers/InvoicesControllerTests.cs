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
            .Setup(invoiceService => invoiceService.SearchAsync("ACME", invoiceDateFrom, invoiceDateTo, "customer", "asc", 1, 5))
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

    var result = await controller.Index("ACME", invoiceDateFrom, invoiceDateTo, "customer", "asc");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<InvoiceSearchViewModel>(viewResult.Model);
        Assert.Equal("ACME", model.SearchTerm);
        Assert.Equal(invoiceDateFrom, model.InvoiceDateFrom);
        Assert.Equal(invoiceDateTo, model.InvoiceDateTo);
        Assert.Equal("customer", model.SortBy);
        Assert.Equal("asc", model.SortDirection);
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
            .Setup(invoiceService => invoiceService.SearchAsync(null, null, null, "updated", "desc", 1, 5))
            .ReturnsAsync(new PagedResult<InvoiceListItemViewModel>());

        var controller = new InvoicesController(service.Object);

        await controller.Index(null, null, null, null, null, 0);

        service.Verify(invoiceService => invoiceService.SearchAsync(null, null, null, "updated", "desc", 1, 5), Times.Once);
    }

    [Fact]
    public async Task Index_WhenPageIsBeyondLastPage_ReturnsEmptyPage()
    {
        var service = new Mock<IInvoiceService>();
        service
            .Setup(invoiceService => invoiceService.SearchAsync("ACME", null, null, "status", "desc", 9, 5))
            .ReturnsAsync(new PagedResult<InvoiceListItemViewModel>
            {
                TotalCount = 21
            });

        var controller = new InvoicesController(service.Object);

        var result = await controller.Index("ACME", null, null, "status", "desc", 9);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<InvoiceSearchViewModel>(viewResult.Model);
        Assert.Equal(9, model.PageNumber);
        Assert.Empty(model.Invoices);
        Assert.Equal(21, model.TotalCount);
        service.Verify(invoiceService => invoiceService.SearchAsync("ACME", null, null, "status", "desc", 9, 5), Times.Once);
    }
}