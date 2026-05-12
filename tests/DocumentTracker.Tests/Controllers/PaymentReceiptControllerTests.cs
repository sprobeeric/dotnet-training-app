using DocumentTracker.Controllers;
using DocumentTracker.Services;
using DocumentTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DocumentTracker.Tests.Controllers;

public class PaymentReceiptControllerTests
{
    [Fact]
    public async Task Index_WithoutPagingParameters_UsesDefaultPagingValues()
    {
        var service = new Mock<IPaymentReceiptService>();
        service
            .Setup(paymentReceiptService => paymentReceiptService.SearchAsync(null, null, null, "payment_date_utc", "desc", 1, 10))
            .ReturnsAsync(new PaginatedResult<PaymentReceiptListItemViewModel>());

        var controller = new PaymentReceiptController(service.Object);

        var result = await controller.Index(null, null, null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PaymentReceiptSearchViewModel>(viewResult.Model);
        Assert.Equal(1, model.Page);
        Assert.Equal(10, model.PageSize);
        service.Verify(paymentReceiptService => paymentReceiptService.SearchAsync(null, null, null, "payment_date_utc", "desc", 1, 10), Times.Once);
    }
}
