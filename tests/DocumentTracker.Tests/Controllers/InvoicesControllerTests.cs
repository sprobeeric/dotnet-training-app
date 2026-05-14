using DocumentTracker.Controllers;
using DocumentTracker.Models;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class InvoicesControllerTests
{
    private static InvoicesController CreateController(
        IInvoiceService service,
        ICurrentUserRoleProvider? roleProvider = null,
        ILogger<InvoicesController>? logger = null) =>
        new(service,
            roleProvider ?? new Mock<ICurrentUserRoleProvider>().Object,
            logger ?? new Mock<ILogger<InvoicesController>>().Object);

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

        var controller = CreateController(service.Object);

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

        var controller = CreateController(service.Object);

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

        var controller = CreateController(service.Object);

        var result = await controller.Index("ACME", null, null, "status", "desc", 9);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<InvoiceSearchViewModel>(viewResult.Model);
        Assert.Equal(9, model.PageNumber);
        Assert.Empty(model.Invoices);
        Assert.Equal(21, model.TotalCount);
        service.Verify(invoiceService => invoiceService.SearchAsync("ACME", null, null, "status", "desc", 9, 5), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenInvoiceNotFound_ReturnsNotFound()
    {
        var service = new Mock<IInvoiceService>();
        service.Setup(s => s.GetDeleteAsync(42)).ReturnsAsync((InvoiceDeleteViewModel?)null);

        var controller = CreateController(service.Object);

        var result = await controller.Delete(42);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_WhenInvoiceExists_ReturnsViewWithViewModel()
    {
        var viewModel = new InvoiceDeleteViewModel { Id = 7, InvoiceNumber = "INV-007" };
        var service = new Mock<IInvoiceService>();
        service.Setup(s => s.GetDeleteAsync(7)).ReturnsAsync(viewModel);

        var controller = CreateController(service.Object);

        var result = await controller.Delete(7);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenSucceeds_RedirectsToIndex()
    {
        var service = new Mock<IInvoiceService>();
        service.Setup(s => s.SoftDeleteAsync(3, It.IsAny<string?>())).ReturnsAsync(ServiceResult.Success());

        var controller = CreateController(service.Object);

        var result = await controller.DeleteConfirmed(3);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(InvoicesController.Index), redirect.ActionName);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenServiceFails_AndInvoiceExists_ReturnsViewWithViewModel()
    {
        var viewModel = new InvoiceDeleteViewModel { Id = 3, InvoiceNumber = "INV-003" };
        var service = new Mock<IInvoiceService>();
        service.Setup(s => s.SoftDeleteAsync(3, It.IsAny<string?>()))
            .ReturnsAsync(ServiceResult.Failure(string.Empty, "This invoice has already been deleted."));
        service.Setup(s => s.GetDeleteAsync(3)).ReturnsAsync(viewModel);

        var controller = CreateController(service.Object);

        var result = await controller.DeleteConfirmed(3);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenServiceFails_AndInvoiceGone_ReturnsErrorMessageView()
    {
        var service = new Mock<IInvoiceService>();
        service.Setup(s => s.SoftDeleteAsync(3, It.IsAny<string?>()))
            .ReturnsAsync(ServiceResult.Failure(string.Empty, "The invoice could not be deleted."));
        service.Setup(s => s.GetDeleteAsync(3)).ReturnsAsync((InvoiceDeleteViewModel?)null);

        var controller = CreateController(service.Object);

        var result = await controller.DeleteConfirmed(3);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("ErrorMessage", viewResult.ViewName);
    }
}
