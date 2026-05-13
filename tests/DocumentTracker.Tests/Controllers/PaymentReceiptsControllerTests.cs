using DocumentTracker.Controllers;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class PaymentReceiptsControllerTests
{
    [Fact]
    public async Task Details_WhenReceiptIsMissing_ReturnsNotFound()
    {
        var service = new Mock<IPaymentReceiptService>();
        var controller = new PaymentReceiptsController(service.Object);

        service
            .Setup(paymentReceiptService => paymentReceiptService.GetDetailsAsync(5))
            .ReturnsAsync((PaymentReceiptDetailsViewModel?)null);

        var result = await controller.Details(5);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_WhenReceiptExists_ReturnsDetailsView()
    {
        var service = new Mock<IPaymentReceiptService>();
        var controller = new PaymentReceiptsController(service.Object);
        var details = new PaymentReceiptDetailsViewModel
        {
            Id = 5,
            ReceiptNumber = "PR-1001",
            InvoiceNumber = "INV-1001",
            CustomerName = "Northwind Traders"
        };

        service
            .Setup(paymentReceiptService => paymentReceiptService.GetDetailsAsync(5))
            .ReturnsAsync(details);

        var result = await controller.Details(5);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(details, viewResult.Model);
    }

    [Fact]
    public async Task Index_WithoutPagingParameters_UsesDefaultPagingValues()
    {
        var service = new Mock<IPaymentReceiptService>();
        service
            .Setup(paymentReceiptService => paymentReceiptService.SearchAsync(null, null, null, "payment_date", "desc", 1, 10))
            .ReturnsAsync(new PaginatedResult<PaymentReceiptListItemViewModel>());

        var controller = new PaymentReceiptsController(service.Object);

        var result = await controller.Index(null, null, null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaymentReceiptSearchViewModel>(viewResult.Model);
        Assert.Equal(1, model.Page);
        Assert.Equal(10, model.PageSize);
        service.Verify(paymentReceiptService => paymentReceiptService.SearchAsync(null, null, null, "payment_date", "desc", 1, 10), Times.Once);
    }
}
